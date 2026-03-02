using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankOfGraeme.Api.Services;

/// <summary>
/// Handles settling pending transactions and creating balance snapshots.
/// Lives in Domain so both API and Functions can use it.
/// </summary>
public class SettlementService(BankDbContext db, IDateTimeProvider dateTime, ILogger<SettlementService> logger)
{
    /// <summary>
    /// Settle all pending transactions created on or before the cutoff date.
    /// Updates ledger balance for each affected account.
    /// </summary>
    public async Task<int> SettlePendingTransactionsAsync(DateTime cutoffDate)
    {
        var pendingTxns = await db.Transactions
            .Include(t => t.Account)
            .Where(t => t.Status == TransactionStatus.Pending && t.CreatedAt <= cutoffDate)
            .ToListAsync();

        var now = dateTime.UtcNow;
        foreach (var txn in pendingTxns)
        {
            txn.Account.Balance += txn.Amount;
            txn.Status = TransactionStatus.Settled;
            txn.SettledAt = now;
        }

        if (pendingTxns.Count > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Settled {Count} pending transactions (cutoff: {Cutoff})", pendingTxns.Count, cutoffDate);
        }

        return pendingTxns.Count;
    }

    /// <summary>
    /// Create EOD balance snapshots for all active accounts.
    /// Idempotent: skips accounts that already have a snapshot for the given date.
    /// </summary>
    public async Task<int> CreateBalanceSnapshotsAsync(DateOnly date)
    {
        var existingSnapshots = await db.AccountBalanceSnapshots
            .Where(s => s.SnapshotDate == date)
            .Select(s => s.AccountId)
            .ToHashSetAsync();

        var activeAccounts = await db.Accounts
            .Where(a => a.IsActive)
            .Select(a => new { a.Id, a.Balance })
            .ToListAsync();

        var snapshotCutoff = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var pendingByAccount = await db.Transactions
            .Where(t => t.Status == TransactionStatus.Pending && t.Amount < 0 && t.CreatedAt <= snapshotCutoff)
            .GroupBy(t => t.AccountId)
            .Select(g => new { AccountId = g.Key, PendingSum = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.AccountId, x => x.PendingSum);

        int snapshotted = 0;
        foreach (var account in activeAccounts)
        {
            if (existingSnapshots.Contains(account.Id)) continue;

            pendingByAccount.TryGetValue(account.Id, out var pendingSum);

            db.AccountBalanceSnapshots.Add(new AccountBalanceSnapshot
            {
                AccountId = account.Id,
                SnapshotDate = date,
                LedgerBalance = account.Balance,
                AvailableBalance = account.Balance + pendingSum,
            });
            snapshotted++;
        }

        if (snapshotted > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Created {Count} balance snapshots for {Date}", snapshotted, date);
        }

        return snapshotted;
    }
}
