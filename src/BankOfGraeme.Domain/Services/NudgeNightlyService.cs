using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankOfGraeme.Api.Services;

/// <summary>
/// Nightly nudge processing: expires stale PENDING nudges and generates fresh ones.
///
/// DESIGN DECISIONS:
///
/// 1. PENDING nudges expire after 3 days.
///    Rationale: a nudge saying "rent due in 2 days" becomes misleading after
///    the payment date passes. The customer's financial context changes daily
///    (balance, transactions, upcoming payments), so stale nudges risk being
///    inaccurate. 3 days gives customers time to act while keeping nudges
///    relevant. Expired nudges are marked EXPIRED (not deleted) for audit.
///
/// 2. Signals are NOT persisted — they are recalculated each run.
///    Rationale: signals are derived from current state (balance, transactions,
///    upcoming payments). Storing them would create stale data that diverges from
///    reality. The context_snapshot on each nudge captures the signals at
///    generation time for audit purposes.
///
/// 3. Nightly batch processes ALL customers (not a sample).
///    Most customers will be skipped quickly (no signals = no API call).
///    Only customers with active signals trigger an LLM call.
///
/// 4. Nudge fatigue limit is 3 per customer per rolling 7 days.
///    This is checked inside the NudgeGenerator before calling the LLM.
/// </summary>
public class NudgeNightlyService(
    BankDbContext db,
    NudgeBatchRunner batchRunner,
    IDateTimeProvider dateTime,
    ILogger<NudgeNightlyService> logger)
{
    private const int PendingExpiryDays = 3;

    public async Task<NightlyNudgeResult> ProcessAsync()
    {
        var now = dateTime.UtcNow;

        // 1. Expire stale PENDING nudges (older than 3 days)
        var expiryCutoff = now.AddDays(-PendingExpiryDays);
        var staleNudges = await db.Nudges
            .Where(n => n.Status == NudgeStatus.PENDING && n.CreatedAt < expiryCutoff)
            .ToListAsync();

        foreach (var nudge in staleNudges)
        {
            nudge.Status = NudgeStatus.EXPIRED;
        }

        if (staleNudges.Count > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Expired {Count} stale PENDING nudges older than {Days} days",
                staleNudges.Count, PendingExpiryDays);
        }

        // 2. Generate fresh nudges for all customers
        var batchResult = await batchRunner.RunAsync();

        logger.LogInformation(
            "Nightly nudge run complete: {Generated} generated, {Skipped} skipped, {Errors} errors, {Expired} expired",
            batchResult.Generated, batchResult.Skipped, batchResult.Errors, staleNudges.Count);

        return new NightlyNudgeResult(
            Expired: staleNudges.Count,
            Generated: batchResult.Generated,
            Skipped: batchResult.Skipped,
            Errors: batchResult.Errors,
            Total: batchResult.Total);
    }
}

public record NightlyNudgeResult(int Expired, int Generated, int Skipped, int Errors, int Total);
