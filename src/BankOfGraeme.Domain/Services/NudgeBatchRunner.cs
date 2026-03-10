using System.Text.Json;
using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankOfGraeme.Api.Services;

public record BatchRunResult(int Generated, int Skipped, int Errors, int Total, List<string> SkipReasons);

public class NudgeBatchRunner(
    BankDbContext db,
    NudgeContextAssembler contextAssembler,
    NudgeGenerator nudgeGenerator,
    ILogger<NudgeBatchRunner> logger)
{
    public async Task<BatchRunResult> RunAsync(int? sampleSize = null, List<int>? customerIds = null)
    {
        List<int> targetCustomers;

        if (customerIds is { Count: > 0 })
        {
            targetCustomers = customerIds;
        }
        else if (sampleSize.HasValue)
        {
            targetCustomers = await db.Customers
                .Select(c => c.Id)
                .OrderBy(c => Guid.NewGuid()) // random sample
                .Take(sampleSize.Value)
                .ToListAsync();
        }
        else
        {
            targetCustomers = await db.Customers
                .Select(c => c.Id)
                .ToListAsync();
        }

        var generated = 0;
        var skipped = 0;
        var errors = 0;
        var skipReasons = new List<string>();

        foreach (var customerId in targetCustomers)
        {
            try
            {
                var context = await contextAssembler.AssembleAsync(customerId);
                if (context is null)
                {
                    var reason = $"Customer {customerId}: context assembly returned null (customer not found)";
                    logger.LogInformation("{SkipReason}", reason);
                    skipReasons.Add(reason);
                    skipped++;
                    continue;
                }

                if (context.Signals.Count == 0)
                {
                    var reason = $"Customer {customerId} ({context.Customer.Name}): no signals detected — balance ${context.Financial.CurrentBalance:F2}, {context.Upcoming.Count} upcoming payments";
                    logger.LogInformation("{SkipReason}", reason);
                    skipReasons.Add(reason);
                    skipped++;
                    continue;
                }

                logger.LogInformation("Customer {CustomerId} ({Name}): {SignalCount} signals detected — {Signals}",
                    customerId, context.Customer.Name, context.Signals.Count,
                    string.Join(", ", context.Signals.Select(s => s.Type.ToString())));

                var outcome = await nudgeGenerator.GenerateAsync(context);
                if (outcome.Nudge is null)
                {
                    var reason = $"Customer {customerId} ({context.Customer.Name}): generator skipped — {outcome.SkipReason}";
                    logger.LogInformation("{SkipReason}", reason);
                    skipReasons.Add(reason);
                    skipped++;
                    continue;
                }

                var nudgeResult = outcome.Nudge;

                var nudge = new Nudge
                {
                    CustomerId = customerId,
                    Message = nudgeResult.Message,
                    Cta = nudgeResult.Cta,
                    Urgency = Enum.Parse<NudgeUrgency>(nudgeResult.Urgency),
                    Category = Enum.Parse<NudgeCategory>(nudgeResult.Category),
                    Reasoning = nudgeResult.Reasoning,
                    Status = NudgeStatus.PENDING,
                    ContextSnapshot = JsonSerializer.Serialize(context)
                };

                db.Nudges.Add(nudge);
                await db.SaveChangesAsync();

                generated++;
                logger.LogInformation("Generated nudge for customer {CustomerId} ({Name}): {Category} — \"{Message}\"",
                    customerId, context.Customer.Name, nudgeResult.Category, nudgeResult.Message);
            }
            catch (Exception ex)
            {
                var reason = $"Customer {customerId}: exception — {ex.Message}";
                logger.LogError(ex, "Error processing nudge for customer {CustomerId}", customerId);
                skipReasons.Add(reason);
                errors++;
            }
        }

        return new BatchRunResult(generated, skipped, errors, targetCustomers.Count, skipReasons);
    }
}
