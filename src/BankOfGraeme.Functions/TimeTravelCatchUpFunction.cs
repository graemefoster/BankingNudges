using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankOfGraeme.Functions;

/// <summary>
/// Polls every 5 seconds and processes any unprocessed days between
/// LastProcessedDate and the current virtual date.
/// This handles both time-travel advances and real nightly catch-up.
/// Each day's settlement, accrual, posting, snapshot and checkpoint are
/// committed in a single transaction — everything commits or nothing does.
/// </summary>
public class TimeTravelCatchUpFunction(
    InterestCalculationService interestService,
    SettlementService settlementService,
    IDateTimeProvider dateTimeProvider,
    BankDbContext db,
    ILogger<TimeTravelCatchUpFunction> logger)
{
    [Function("TimeTravelCatchUp")]
    public async Task Run(
        [TimerTrigger("*/5 * * * * *")] TimerInfo timer)
    {
        var virtualToday = dateTimeProvider.Today;

        var lastProcessedSetting = await db.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == "LastProcessedDate");

        var lastProcessed = lastProcessedSetting is not null
            ? DateOnly.Parse(lastProcessedSetting.Value)
            : virtualToday.AddDays(-1);

        // Handle backward time jumps (e.g. after reset)
        if (lastProcessed > virtualToday)
        {
            logger.LogInformation(
                "LastProcessedDate {Last} is ahead of virtual today {Today}, clamping",
                lastProcessed, virtualToday);
            lastProcessed = virtualToday.AddDays(-1);
        }

        if (lastProcessed >= virtualToday)
            return;

        var daysToProcess = virtualToday.DayNumber - lastProcessed.DayNumber;
        logger.LogInformation(
            "Time-travel catch-up: processing {Days} day(s) from {From} to {To}",
            daysToProcess, lastProcessed.AddDays(1), virtualToday);

        for (var date = lastProcessed.AddDays(1); date <= virtualToday; date = date.AddDays(1))
        {
            // Wrap all nightly operations in one transaction
            await using var txn = await db.Database.BeginTransactionAsync();

            // 1. Settle pending transactions older than 2 days
            var cutoffDate = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(-2);
            var settled = await settlementService.SettlePendingTransactionsAsync(cutoffDate);
            if (settled > 0)
                logger.LogInformation("Settled {Count} pending transactions for {Date}", settled, date);

            // 2. Interest accrual and posting
            await interestService.AccrueDailyInterestAsync(date);
            await interestService.PostMonthlyInterestAsync(date);

            // 3. Create EOD balance snapshots for all active accounts
            await settlementService.CreateBalanceSnapshotsAsync(date);

            // 4. Update checkpoint
            var checkpoint = await db.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "LastProcessedDate");

            if (checkpoint is null)
            {
                db.SystemSettings.Add(new SystemSettings
                {
                    Key = "LastProcessedDate",
                    Value = date.ToString("yyyy-MM-dd")
                });
            }
            else
            {
                checkpoint.Value = date.ToString("yyyy-MM-dd");
            }

            await db.SaveChangesAsync();
            await txn.CommitAsync();
        }

        logger.LogInformation("Time-travel catch-up complete. LastProcessedDate = {Date}", virtualToday);
    }
}
