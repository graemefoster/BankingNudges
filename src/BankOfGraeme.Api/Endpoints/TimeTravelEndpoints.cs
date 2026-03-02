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

        group.MapPost("/advance", async (AdvanceRequest req, IDateTimeProvider dateTime) =>
        {
            if (req.Days <= 0)
                return Results.BadRequest(new { error = "Days must be positive" });

            await dateTime.AdvanceDaysAsync(req.Days);

            return Results.Ok(new
            {
                virtualUtcNow = dateTime.UtcNow,
                virtualToday = dateTime.Today.ToString("yyyy-MM-dd"),
                daysAdvanced = dateTime.DaysAdvanced,
                daysJustAdvanced = req.Days,
                note = "Interest accrual will be processed by the Functions app within a few seconds."
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
