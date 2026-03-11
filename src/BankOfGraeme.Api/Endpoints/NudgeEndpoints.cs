using System.Text.Json;
using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Endpoints;

public static class NudgeEndpoints
{
    public static void MapNudgeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/nudges").WithTags("Nudges");

        // GET /api/nudges/:customerId — most recent PENDING nudge
        group.MapGet("/{customerId:int}", async (int customerId, BankDbContext db) =>
        {
            var nudge = await db.Nudges
                .AsNoTracking()
                .Where(n => n.CustomerId == customerId && n.Status == NudgeStatus.PENDING)
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync();

            if (nudge is null) return Results.NotFound();

            return Results.Ok(new
            {
                nudge.Id,
                nudge.Message,
                nudge.Cta,
                Urgency = nudge.Urgency.ToString(),
                Category = nudge.Category.ToString()
            });
        });

        // POST /api/nudges/generate/:customerId — on-demand nudge generation
        group.MapPost("/generate/{customerId:int}", async (
            int customerId,
            BankDbContext db,
            IDateTimeProvider dateTime,
            NudgeContextAssembler contextAssembler,
            NudgeGenerator nudgeGenerator) =>
        {
            var today = dateTime.Today;
            var startOfDay = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var endOfDay = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            // Return existing nudge if one was already generated today (any status)
            var existing = await db.Nudges
                .AsNoTracking()
                .Where(n => n.CustomerId == customerId
                    && n.CreatedAt >= startOfDay
                    && n.CreatedAt < endOfDay)
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync();

            if (existing is not null)
            {
                return Results.Ok(new NudgeGenerateResponse(
                    Generated: true,
                    Nudge: new NudgeDto(
                        existing.Id,
                        existing.Message,
                        existing.Cta,
                        existing.Urgency.ToString(),
                        existing.Category.ToString()),
                    Reason: null));
            }

            // Assemble context and detect signals
            var context = await contextAssembler.AssembleAsync(customerId);
            if (context is null)
                return Results.NotFound(new NudgeGenerateResponse(false, null, "Customer not found"));

            if (context.Signals.Count == 0)
                return Results.Ok(new NudgeGenerateResponse(false, null, "No insights right now — your finances look on track \ud83d\udc4d"));

            // Generate via LLM
            var outcome = await nudgeGenerator.GenerateAsync(context);
            if (outcome.Nudge is null)
            {
                // Fatigue or other skip — show the most recent nudge from this week instead
                var recent = await db.Nudges
                    .AsNoTracking()
                    .Where(n => n.CustomerId == customerId && n.CreatedAt >= dateTime.UtcNow.AddDays(-7))
                    .OrderByDescending(n => n.CreatedAt)
                    .FirstOrDefaultAsync();

                if (recent is not null)
                {
                    return Results.Ok(new NudgeGenerateResponse(
                        Generated: true,
                        Nudge: new NudgeDto(
                            recent.Id,
                            recent.Message,
                            recent.Cta,
                            recent.Urgency.ToString(),
                            recent.Category.ToString()),
                        Reason: null));
                }

                return Results.Ok(new NudgeGenerateResponse(false, null, "No insights right now — your finances look on track 👍"));
            }

            var nudge = new Nudge
            {
                CustomerId = customerId,
                Message = outcome.Nudge.Message,
                Cta = outcome.Nudge.Cta,
                Urgency = Enum.Parse<NudgeUrgency>(outcome.Nudge.Urgency),
                Category = Enum.Parse<NudgeCategory>(outcome.Nudge.Category),
                Reasoning = outcome.Nudge.Reasoning,
                Status = NudgeStatus.PENDING,
                ContextSnapshot = JsonSerializer.Serialize(context)
            };

            db.Nudges.Add(nudge);
            await db.SaveChangesAsync();

            return Results.Ok(new NudgeGenerateResponse(
                Generated: true,
                Nudge: new NudgeDto(
                    nudge.Id,
                    nudge.Message,
                    nudge.Cta,
                    nudge.Urgency.ToString(),
                    nudge.Category.ToString()),
                Reason: null));
        });

        // POST /api/nudges/:nudgeId/respond
        group.MapPost("/{nudgeId:int}/respond", async (int nudgeId, NudgeRespondRequest req, BankDbContext db, IDateTimeProvider dateTime) =>
        {
            var nudge = await db.Nudges.FindAsync(nudgeId);
            if (nudge is null) return Results.NotFound();

            if (!Enum.TryParse<NudgeStatus>(req.Action, true, out var newStatus) ||
                newStatus is not (NudgeStatus.ACCEPTED or NudgeStatus.DISMISSED or NudgeStatus.SNOOZED))
            {
                return Results.BadRequest(new { error = "Action must be ACCEPTED, DISMISSED, or SNOOZED" });
            }

            nudge.Status = newStatus;
            nudge.RespondedAt = dateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok();
        });

        // POST /api/nudges/batch/run
        group.MapPost("/batch/run", async (BatchRunRequest? req, NudgeBatchRunner runner) =>
        {
            var result = await runner.RunAsync(
                sampleSize: req?.SampleSize,
                customerIds: req?.CustomerIds);

            return Results.Ok(new
            {
                result.Generated,
                result.Skipped,
                result.Errors,
                result.Total,
                result.SkipReasons
            });
        });

        // GET /api/nudges/:nudgeId/insight
        group.MapGet("/{nudgeId:int}/insight", async (int nudgeId, BankDbContext db) =>
        {
            var nudge = await db.Nudges
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == nudgeId);

            if (nudge is null) return Results.NotFound();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var context = JsonSerializer.Deserialize<CustomerContext>(nudge.ContextSnapshot, options);

            if (context is null)
                return Results.Problem("Unable to parse nudge context");

            var response = new NudgeInsightResponse(
                Nudge: new NudgeDetailDto(
                    nudge.Id, nudge.Message, nudge.Cta,
                    nudge.Urgency.ToString(), nudge.Category.ToString(),
                    nudge.Reasoning, nudge.Status.ToString(),
                    nudge.CreatedAt, nudge.RespondedAt),
                Context: new NudgeInsightContext(
                    Financial: new NudgeInsightFinancial(
                        context.Financial.CurrentBalance,
                        context.Financial.AvgMonthlyIncome,
                        context.Financial.SpendByCategory,
                        context.Financial.SpendDelta,
                        context.Financial.DaysUntilLikelyPayday,
                        context.Financial.Accounts?.Select(a => new NudgeInsightAccount(
                            a.Name, a.AccountType, a.Balance, a.InterestRate, a.BonusInterestRate)).ToList()),
                    Upcoming: context.Upcoming.Select(u => new NudgeInsightPayment(
                        u.Merchant, u.Amount, u.DueInDays, u.Confidence, u.Source)).ToList(),
                    Signals: context.Signals.Select(s => new NudgeInsightSignal(
                        s.Type.ToString(), s.Severity.ToString(), s.Category, s.Delta,
                        s.PaymentMerchant, s.PaymentAmount, s.DueInDays)).ToList()));

            return Results.Ok(response);
        });

        // GET /api/nudges/:customerId/history
        group.MapGet("/{customerId:int}/history", async (int customerId, BankDbContext db) =>
        {
            var nudges = await db.Nudges
                .AsNoTracking()
                .Where(n => n.CustomerId == customerId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new
                {
                    n.Id,
                    n.Message,
                    n.Cta,
                    Urgency = n.Urgency.ToString(),
                    Category = n.Category.ToString(),
                    Status = n.Status.ToString(),
                    n.Reasoning,
                    n.CreatedAt,
                    n.RespondedAt
                })
                .ToListAsync();

            return Results.Ok(nudges);
        });

        // GET /api/customers/:customerId/context — debug endpoint
        var customerGroup = app.MapGroup("/api/customers").WithTags("Customers");
        customerGroup.MapGet("/{customerId:int}/context", async (int customerId, NudgeContextAssembler assembler) =>
        {
            var context = await assembler.AssembleAsync(customerId);
            if (context is null) return Results.NotFound();
            return Results.Ok(context);
        });
    }
}

public record NudgeRespondRequest(string Action);
public record BatchRunRequest(int? SampleSize, List<int>? CustomerIds);
public record NudgeDto(int Id, string Message, string Cta, string Urgency, string Category);
public record NudgeGenerateResponse(bool Generated, NudgeDto? Nudge, string? Reason);
public record NudgeInsightResponse(NudgeDetailDto Nudge, NudgeInsightContext Context);
public record NudgeDetailDto(int Id, string Message, string Cta, string Urgency, string Category, string Reasoning, string Status, DateTime CreatedAt, DateTime? RespondedAt);
public record NudgeInsightContext(NudgeInsightFinancial Financial, List<NudgeInsightPayment> Upcoming, List<NudgeInsightSignal> Signals);
public record NudgeInsightFinancial(decimal CurrentBalance, decimal AvgMonthlyIncome, Dictionary<string, decimal> SpendByCategory, Dictionary<string, double> SpendDelta, int DaysUntilLikelyPayday, List<NudgeInsightAccount>? Accounts = null);
public record NudgeInsightAccount(string Name, string AccountType, decimal Balance, decimal? InterestRate, decimal? BonusInterestRate);
public record NudgeInsightPayment(string Merchant, decimal Amount, int DueInDays, string Confidence, string Source);
public record NudgeInsightSignal(string Type, string Severity, string? Category, double? Delta, string? PaymentMerchant, decimal? PaymentAmount, int? DueInDays);
