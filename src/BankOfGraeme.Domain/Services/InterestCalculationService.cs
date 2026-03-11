using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services.InterestCalculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankOfGraeme.Api.Services;

public class InterestCalculationService(
    BankDbContext db,
    IEnumerable<IAccountInterestCalculator> calculators,
    ILogger<InterestCalculationService> logger)
{
    /// <summary>
    /// Calculate and store daily interest accrual for all eligible accounts.
    /// Delegates per-account-type logic to <see cref="IAccountInterestCalculator"/> implementations.
    /// Idempotent: skips accounts that already have an accrual for the given date.
    /// Handles unique constraint violations gracefully for concurrent runs.
    /// </summary>
    public async Task AccrueDailyInterestAsync(DateOnly accrualDate)
    {
        var existingAccruals = await db.InterestAccruals
            .Where(ia => ia.AccrualDate == accrualDate)
            .Select(ia => ia.AccountId)
            .ToHashSetAsync();

        int accrued = 0;

        foreach (var calculator in calculators)
        {
            var accounts = await calculator.GetEligibleAccounts(db).ToListAsync();

            foreach (var account in accounts)
            {
                if (existingAccruals.Contains(account.Id)) continue;

                var dailyInterest = calculator.CalculateDailyInterest(account);
                if (dailyInterest == 0) continue;

                db.InterestAccruals.Add(new InterestAccrual
                {
                    AccountId = account.Id,
                    AccrualDate = accrualDate,
                    DailyAmount = dailyInterest,
                    Posted = false
                });
                accrued++;
            }
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
                logger.LogWarning("Concurrent accrual detected for {Date}, treating as idempotent", accrualDate);
                foreach (var entry in db.ChangeTracker.Entries<InterestAccrual>().ToList())
                    entry.State = EntityState.Detached;
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
    /// For savings accounts with bonus interest, the bonus is forfeited if the
    /// account balance dropped during the month (Australian-style conditional rate).
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

        // Pre-load savings accounts with bonus rates for eligibility checks
        var accountIds = unpostedAccruals.Select(ia => ia.AccountId).Distinct().ToList();
        var savingsAccounts = await db.Accounts
            .Where(a => accountIds.Contains(a.Id)
                        && a.AccountType == AccountType.Savings
                        && a.BonusInterestRate != null
                        && a.InterestRate != null)
            .ToDictionaryAsync(a => a.Id);

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

            // Savings bonus eligibility check: forfeit bonus if balance dropped during the month
            if (savingsAccounts.TryGetValue(accountId, out var savingsAccount))
            {
                var accrualMonth = new DateOnly(group.Key.Year, group.Key.Month, 1);
                var bonusEarned = await IsSavingsBonusEarnedAsync(accountId, accrualMonth);

                if (!bonusEarned)
                {
                    var baseRate = savingsAccount.InterestRate!.Value - savingsAccount.BonusInterestRate!.Value;
                    var fullRate = savingsAccount.InterestRate!.Value;
                    var baseOnlyInterest = Math.Round(totalInterest * (baseRate / fullRate), 2);

                    logger.LogInformation(
                        "Savings account {AccountId} forfeited bonus for {Month}: {Full} → {Base}",
                        accountId, monthLabel, totalInterest, baseOnlyInterest);

                    totalInterest = baseOnlyInterest;
                    if (totalInterest == 0) continue;
                }
            }

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
    /// Determines whether a savings account earned its bonus interest for a given month.
    /// Bonus is earned if the end-of-month balance ≥ start-of-month balance.
    /// Uses AccountBalanceSnapshots (created nightly) to determine month boundaries.
    /// </summary>
    private async Task<bool> IsSavingsBonusEarnedAsync(int accountId, DateOnly monthStart)
    {
        var lastDayOfMonth = monthStart.AddMonths(1).AddDays(-1);

        // Get the snapshot closest to the start of the month (last day of previous month or first day)
        var startSnapshot = await db.AccountBalanceSnapshots
            .Where(s => s.AccountId == accountId && s.SnapshotDate <= monthStart)
            .OrderByDescending(s => s.SnapshotDate)
            .FirstOrDefaultAsync();

        // Get the snapshot for the end of the month
        var endSnapshot = await db.AccountBalanceSnapshots
            .Where(s => s.AccountId == accountId && s.SnapshotDate <= lastDayOfMonth)
            .OrderByDescending(s => s.SnapshotDate)
            .FirstOrDefaultAsync();

        if (startSnapshot is null || endSnapshot is null)
        {
            // No snapshots available — grant bonus (benefit of the doubt for new accounts)
            return true;
        }

        return endSnapshot.LedgerBalance >= startSnapshot.LedgerBalance;
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
