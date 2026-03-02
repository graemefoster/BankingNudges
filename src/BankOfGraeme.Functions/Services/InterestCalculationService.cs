using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankOfGraeme.Functions.Services;

public class InterestCalculationService(BankDbContext db, ILogger<InterestCalculationService> logger)
{
    /// <summary>
    /// Calculate and store daily interest accrual for all eligible accounts.
    /// Idempotent: skips accounts that already have an accrual for the given date.
    /// Handles unique constraint violations gracefully for concurrent runs.
    /// </summary>
    public async Task AccrueDailyInterestAsync(DateOnly accrualDate)
    {
        var homeLoans = await db.Accounts
            .Include(a => a.OffsetAccounts)
            .Where(a => a.IsActive && a.AccountType == AccountType.HomeLoan && a.InterestRate != null)
            .ToListAsync();

        var savings = await db.Accounts
            .Where(a => a.IsActive && a.AccountType == AccountType.Savings && a.InterestRate != null)
            .ToListAsync();

        var existingAccruals = await db.InterestAccruals
            .Where(ia => ia.AccrualDate == accrualDate)
            .Select(ia => ia.AccountId)
            .ToHashSetAsync();

        int accrued = 0;

        foreach (var loan in homeLoans)
        {
            if (existingAccruals.Contains(loan.Id)) continue;

            // Only charge interest on debt (negative balance). If in credit, no interest.
            var principal = Math.Max(0, -loan.Balance);
            if (principal == 0) continue;

            var offsetBalance = loan.OffsetAccounts
                .Where(o => o.IsActive)
                .Sum(o => Math.Max(0, o.Balance));
            var effectivePrincipal = Math.Max(0, principal - offsetBalance);
            var dailyRate = loan.InterestRate!.Value / 100m / 365m;
            var dailyInterest = effectivePrincipal * dailyRate;

            // Home loan interest is charged (negative amount — increases debt)
            db.InterestAccruals.Add(new InterestAccrual
            {
                AccountId = loan.Id,
                AccrualDate = accrualDate,
                DailyAmount = -dailyInterest,
                Posted = false
            });
            accrued++;
        }

        foreach (var account in savings)
        {
            if (existingAccruals.Contains(account.Id)) continue;

            var balance = Math.Max(0, account.Balance);
            if (balance == 0) continue;

            var dailyRate = account.InterestRate!.Value / 100m / 365m;
            var dailyInterest = balance * dailyRate;

            // Savings interest is earned (positive amount)
            db.InterestAccruals.Add(new InterestAccrual
            {
                AccountId = account.Id,
                AccrualDate = accrualDate,
                DailyAmount = dailyInterest,
                Posted = false
            });
            accrued++;
        }

        if (accrued > 0)
        {
            try
            {
                await db.SaveChangesAsync();
                logger.LogInformation("Accrued daily interest for {Count} accounts on {Date}", accrued, accrualDate);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                // Concurrent run already inserted accruals — this is benign
                logger.LogWarning("Concurrent accrual detected for {Date}, treating as idempotent", accrualDate);
                db.ChangeTracker.Clear();
            }
        }
        else
        {
            logger.LogInformation("No new accruals needed for {Date} (already processed or no eligible accounts)", accrualDate);
        }
    }

    /// <summary>
    /// Post accumulated interest as transactions for any completed months.
    /// Groups by account AND month to ensure each month gets its own transaction.
    /// All updates (balance, transaction, accrual flags) are committed atomically per account-month.
    /// </summary>
    public async Task PostMonthlyInterestAsync(DateOnly today)
    {
        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);

        var unpostedAccruals = await db.InterestAccruals
            .Where(ia => !ia.Posted && ia.AccrualDate < firstOfMonth)
            .OrderBy(ia => ia.AccountId)
            .ThenBy(ia => ia.AccrualDate)
            .ToListAsync();

        if (unpostedAccruals.Count == 0)
        {
            logger.LogInformation("No unposted accruals to process for months before {Month}", firstOfMonth);
            return;
        }

        // Group by account AND month — each month gets its own transaction
        var grouped = unpostedAccruals.GroupBy(ia => new
        {
            ia.AccountId,
            ia.AccrualDate.Year,
            ia.AccrualDate.Month
        });

        int posted = 0;

        foreach (var group in grouped)
        {
            var accountId = group.Key.AccountId;
            var totalInterest = Math.Round(group.Sum(ia => ia.DailyAmount), 2);

            if (totalInterest == 0) continue;

            var monthLabel = new DateOnly(group.Key.Year, group.Key.Month, 1).ToString("MMMM yyyy");
            var description = totalInterest < 0
                ? $"Interest Charged — {monthLabel}"
                : $"Interest Earned — {monthLabel}";

            var success = await PostInterestAtomicallyAsync(
                accountId, totalInterest, description, group.ToList());

            if (success) posted++;
        }

        if (posted > 0)
        {
            logger.LogInformation("Posted monthly interest for {Count} account-months", posted);
        }
    }

    /// <summary>
    /// Atomically: update account balance, create interest transaction, and mark accruals as posted.
    /// All in one SaveChangesAsync call to prevent double-posting on crash.
    /// Uses navigation property for PostedTransaction to avoid a second save.
    /// </summary>
    private async Task<bool> PostInterestAtomicallyAsync(
        int accountId, decimal amount, string description, List<InterestAccrual> accruals)
    {
        const int maxRetries = 3;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            // Guard against concurrent runners: verify accruals aren't already posted
            if (attempt > 0)
            {
                var alreadyPosted = await db.InterestAccruals
                    .AsNoTracking()
                    .AnyAsync(ia => ia.Id == accruals[0].Id && ia.Posted);
                if (alreadyPosted)
                {
                    logger.LogInformation(
                        "Accruals for account {AccountId} already posted by concurrent run, skipping",
                        accountId);
                    return false;
                }
            }

            // Reload account fresh on each attempt
            var account = await db.Accounts.FindAsync(accountId);
            if (account is null)
            {
                logger.LogWarning("Account {AccountId} not found during interest posting, skipping", accountId);
                return false;
            }

            account.Balance += amount;

            var txn = new Transaction
            {
                AccountId = accountId,
                Amount = amount,
                Description = description,
                TransactionType = TransactionType.Interest,
                BalanceAfter = account.Balance
            };

            db.Transactions.Add(txn);

            // Mark accruals as posted and link to transaction in the SAME save
            foreach (var accrual in accruals)
            {
                accrual.Posted = true;
                accrual.PostedTransaction = txn;
            }

            try
            {
                await db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxRetries - 1)
            {
                logger.LogWarning(
                    "Concurrency conflict posting interest for account {AccountId}, retry {Attempt}",
                    accountId, attempt + 1);

                // Detach only the conflicting entities, not the whole tracker
                db.Entry(account).State = EntityState.Detached;
                db.Entry(txn).State = EntityState.Detached;

                // Reset accrual flags for retry
                foreach (var accrual in accruals)
                {
                    accrual.Posted = false;
                    accrual.PostedTransaction = null;
                }
            }
        }

        logger.LogError("Failed to post interest for account {AccountId} after {MaxRetries} retries", accountId, maxRetries);
        return false;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // PostgreSQL unique violation error code: 23505
        return ex.InnerException?.Message?.Contains("23505") == true
            || ex.InnerException?.Message?.Contains("duplicate key") == true;
    }
}
