using BankOfGraeme.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BankOfGraeme.Functions;

public class NightlyInterestFunction(
    InterestCalculationService interestService,
    ILogger<NightlyInterestFunction> logger)
{
    /// <summary>
    /// Runs nightly at 2:00 AM to accrue daily interest and post monthly totals.
    /// CRON: second minute hour day month day-of-week
    /// </summary>
    [Function("NightlyInterestBatch")]
    public async Task Run(
        [TimerTrigger("0 0 2 * * *")] TimerInfo timer)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        logger.LogInformation("Nightly interest batch started for {Date}", today);

        try
        {
            await interestService.AccrueDailyInterestAsync(today);
            await interestService.PostMonthlyInterestAsync(today);

            logger.LogInformation("Nightly interest batch completed for {Date}", today);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Nightly interest batch failed for {Date}", today);
            throw;
        }

        if (timer.ScheduleStatus is not null)
        {
            logger.LogInformation("Next scheduled run: {Next}", timer.ScheduleStatus.Next);
        }
    }
}
