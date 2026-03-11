using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankOfGraeme.Api.Services;

/// <summary>
/// Executes scheduled payments that are due on or before a given date.
/// Called by the nightly batch (TimeTravelCatchUpFunction) for each processing day.
/// </summary>
public class ScheduledPaymentService(BankDbContext db, ILogger<ScheduledPaymentService> logger)
{
    private const int MaxRetries = 3;

    /// <summary>
    /// Execute all active scheduled payments due on or before the given date.
    /// For each payment: creates transaction(s), advances NextDueDate, deactivates one-offs.
    /// Insufficient funds produces a visible declined transaction.
    /// </summary>
    public async Task<int> ExecuteDuePaymentsAsync(DateOnly date)
    {
        var duePayments = await db.ScheduledPayments
            .Include(sp => sp.Account)
            .Where(sp => sp.IsActive && sp.NextDueDate <= date && sp.Account.IsActive)
            .ToListAsync();

        if (duePayments.Count == 0) return 0;

        int executed = 0;
        var settledAt = date.ToDateTime(TimeOnly.Parse("09:00"), DateTimeKind.Utc);

        foreach (var payment in duePayments)
        {
            // Check if the payment's end date has passed
            if (payment.EndDate.HasValue && date > payment.EndDate.Value)
            {
                payment.IsActive = false;
                continue;
            }

            var description = payment.Description ?? $"Direct Debit - {payment.PayeeName}";

            // If internal payee, verify destination exists before debiting source
            Account? payeeAccount = null;
            if (payment.PayeeAccountId.HasValue)
            {
                payeeAccount = await db.Accounts.FindAsync(payment.PayeeAccountId.Value);
                if (payeeAccount is null || !payeeAccount.IsActive)
                {
                    logger.LogWarning(
                        "Scheduled payment {PaymentId} ({Payee}): payee account {PayeeAccountId} not found or closed, skipping",
                        payment.Id, payment.PayeeName, payment.PayeeAccountId);
                    payment.IsActive = false;
                    continue;
                }
            }

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    // Re-load account on retry to get fresh RowVersion
                    if (attempt > 0)
                    {
                        db.ChangeTracker.Clear();
                        payment.Account = (await db.Accounts.FindAsync(payment.AccountId))!;
                        // Re-attach the payment entity
                        db.ScheduledPayments.Attach(payment);
                        db.Entry(payment).State = EntityState.Modified;
                        if (payeeAccount is not null)
                            payeeAccount = await db.Accounts.FindAsync(payment.PayeeAccountId!.Value);
                    }

                    var account = payment.Account;

                    // Check available balance (ledger minus pending holds)
                    var pendingHolds = await db.Transactions
                        .Where(t => t.AccountId == account.Id && t.Status == TransactionStatus.Pending && t.Amount < 0)
                        .SumAsync(t => t.Amount);
                    var availableBalance = account.Balance + pendingHolds;

                    if (availableBalance < payment.Amount)
                    {
                        db.Transactions.Add(new Transaction
                        {
                            AccountId = payment.AccountId,
                            Amount = 0,
                            Description = description,
                            TransactionType = TransactionType.DirectDebit,
                            Status = TransactionStatus.Failed,
                            FailureReason = "Insufficient funds",
                            CreatedAt = settledAt,
                        });

                        logger.LogWarning(
                            "Scheduled payment {PaymentId} ({Payee}) declined for account {AccountId}: insufficient funds",
                            payment.Id, payment.PayeeName, payment.AccountId);
                    }
                    else
                    {
                        account.Balance -= payment.Amount;
                        db.Transactions.Add(new Transaction
                        {
                            AccountId = payment.AccountId,
                            Amount = -payment.Amount,
                            Description = description,
                            TransactionType = TransactionType.DirectDebit,
                            Status = TransactionStatus.Settled,
                            SettledAt = settledAt,
                            CreatedAt = settledAt,
                        });

                        if (payeeAccount is not null)
                        {
                            payeeAccount.Balance += payment.Amount;
                            db.Transactions.Add(new Transaction
                            {
                                AccountId = payeeAccount.Id,
                                Amount = payment.Amount,
                                Description = payment.Reference ?? $"Payment from {account.Name}",
                                TransactionType = TransactionType.DirectDebit,
                                Status = TransactionStatus.Settled,
                                SettledAt = settledAt,
                                CreatedAt = settledAt,
                            });
                        }

                        executed++;
                        logger.LogInformation(
                            "Executed scheduled payment {PaymentId} ({Payee}) for ${Amount} on account {AccountId}",
                            payment.Id, payment.PayeeName, payment.Amount, payment.AccountId);
                    }

                    AdvanceNextDueDate(payment);
                    await db.SaveChangesAsync();
                    break; // Success — exit retry loop
                }
                catch (DbUpdateConcurrencyException) when (attempt < MaxRetries - 1)
                {
                    logger.LogWarning(
                        "Concurrency conflict on scheduled payment {PaymentId}, attempt {Attempt}",
                        payment.Id, attempt + 1);
                }
            }
        }

        return executed;
    }

    private static void AdvanceNextDueDate(ScheduledPayment payment)
    {
        if (payment.Frequency == ScheduleFrequency.OneOff)
        {
            payment.IsActive = false;
            return;
        }

        var next = payment.Frequency switch
        {
            ScheduleFrequency.Weekly => payment.NextDueDate.AddDays(7),
            ScheduleFrequency.Fortnightly => payment.NextDueDate.AddDays(14),
            ScheduleFrequency.Monthly => payment.NextDueDate.AddMonths(1),
            ScheduleFrequency.Quarterly => payment.NextDueDate.AddMonths(3),
            ScheduleFrequency.Yearly => payment.NextDueDate.AddYears(1),
            _ => payment.NextDueDate.AddMonths(1),
        };

        // If end date is reached, deactivate
        if (payment.EndDate.HasValue && next > payment.EndDate.Value)
        {
            payment.IsActive = false;
            return;
        }

        payment.NextDueDate = next;
    }
}
