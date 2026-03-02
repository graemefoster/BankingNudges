using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Services;

namespace BankOfGraeme.Api.Endpoints;

public static class TimeTravelEndpoints
{
    public static void MapTimeTravelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/time-travel").WithTags("Time Travel");

        group.MapGet("/current", (IDateTimeProvider dateTime) =>
            Results.Ok(new
            {
                virtualUtcNow = dateTime.UtcNow,
                virtualToday = dateTime.Today.ToString("yyyy-MM-dd"),
                daysAdvanced = dateTime.DaysAdvanced,
                realUtcNow = DateTime.UtcNow
            }));

        group.MapPost("/advance", async (AdvanceRequest req, IDateTimeProvider dateTime, BankDbContext db, ILoggerFactory loggerFactory) =>
        {
            if (req.Days <= 0)
                return Results.BadRequest(new { error = "Days must be positive" });

            var startDate = dateTime.Today;

            // Run interest batch for each intermediate day
            var interestService = new InterestCalculationService(db, loggerFactory.CreateLogger<InterestCalculationService>());
            for (int d = 1; d <= req.Days; d++)
            {
                var batchDate = startDate.AddDays(d);
                await interestService.AccrueDailyInterestAsync(batchDate);
                await interestService.PostMonthlyInterestAsync(batchDate);
            }

            await dateTime.AdvanceDaysAsync(req.Days);

            return Results.Ok(new
            {
                virtualUtcNow = dateTime.UtcNow,
                virtualToday = dateTime.Today.ToString("yyyy-MM-dd"),
                daysAdvanced = dateTime.DaysAdvanced,
                daysJustAdvanced = req.Days
            });
        });

        group.MapPost("/reset", async (IDateTimeProvider dateTime) =>
        {
            await dateTime.ResetAsync();
            return Results.Ok(new
            {
                virtualUtcNow = dateTime.UtcNow,
                virtualToday = dateTime.Today.ToString("yyyy-MM-dd"),
                daysAdvanced = dateTime.DaysAdvanced
            });
        });
    }
}

public record AdvanceRequest(int Days);
