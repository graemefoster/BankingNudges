using System.Text.Json;
using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankOfGraeme.Api.Services;

/// <summary>
/// Builds the system prompt for a nudge chat agent.
/// The prompt is pre-seeded with the nudge's financial context so the agent
/// can answer questions without additional round-trips for common queries.
/// Nudge history is available on-demand via the NudgeChatTools tool.
/// </summary>
public class NudgeChatAgent(
    BankDbContext db,
    ILogger<NudgeChatAgent> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Loads a nudge and builds a pre-seeded system prompt containing
    /// the nudge details and full financial context snapshot.
    /// </summary>
    public async Task<NudgeChatSetup?> BuildChatSetupAsync(int nudgeId, int customerId)
    {
        var nudge = await db.Nudges
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == nudgeId && n.CustomerId == customerId);

        if (nudge is null)
        {
            logger.LogWarning("Nudge {NudgeId} not found for customer {CustomerId}", nudgeId, customerId);
            return null;
        }

        CustomerContext? context;
        try
        {
            context = JsonSerializer.Deserialize<CustomerContext>(nudge.ContextSnapshot, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize ContextSnapshot for nudge {NudgeId}", nudgeId);
            return null;
        }

        if (context is null)
        {
            logger.LogError("ContextSnapshot deserialized to null for nudge {NudgeId}", nudgeId);
            return null;
        }

        var systemPrompt = BuildSystemPrompt(nudge, context);
        return new NudgeChatSetup(systemPrompt, customerId);
    }

    private static string BuildSystemPrompt(Nudge nudge, CustomerContext context)
    {
        var accountLines = string.Join("\n",
            (context.Financial.Accounts ?? []).Select(a =>
            {
                var rate = a.InterestRate.HasValue ? $"{a.InterestRate:F2}% p.a." : "n/a";
                var bonus = a.BonusInterestRate.HasValue ? $" + {a.BonusInterestRate:F2}% bonus" : "";
                var offsetNote = a.OffsetHomeLoanRate.HasValue
                    ? $" (offsets home loan at {a.OffsetHomeLoanRate:F2}%)"
                    : "";
                return $"  • {a.Name} ({a.AccountType}): ${a.Balance:F2} — interest {rate}{bonus}{offsetNote}";
            }));

        var spendLines = string.Join("\n",
            context.Financial.SpendByCategory
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp =>
                {
                    var delta = context.Financial.SpendDelta.GetValueOrDefault(kvp.Key, 0);
                    var deltaStr = delta != 0 ? $" ({(delta > 0 ? "+" : "")}{delta * 100:F0}% vs last month)" : "";
                    return $"  • {kvp.Key}: ${kvp.Value:F2}{deltaStr}";
                }));

        var upcomingLines = string.Join("\n",
            context.Upcoming.Select(p =>
                $"  • {p.Merchant}: ${p.Amount:F2} due in {p.DueInDays} day(s) ({p.Confidence})"));

        var signalLines = string.Join("\n",
            context.Signals.Select(s => $"  • {s.Type} ({s.Severity})" + s.Type switch
            {
                SignalType.SPEND_SPIKE => $" — {s.Category} up {s.Delta * 100:F0}%",
                SignalType.PAYMENT_DUE_SOON => $" — {s.PaymentMerchant} ${s.PaymentAmount:F2} in {s.DueInDays}d",
                SignalType.FOREIGN_SPEND_NO_HOLIDAY => $" — spending in {s.Category}, no holiday registered",
                SignalType.FLIGHT_BOOKING_DETECTED => $" — ${s.PaymentAmount:F2} at {s.PaymentMerchant}",
                _ => ""
            }));

        return $"""
            You are a helpful financial information assistant for a retail bank called Bank of Graeme.

            A customer is chatting with you about a financial insight (nudge) they received.
            Your job is to explain the nudge, answer questions about the customer's finances,
            and provide context — all using ONLY the data provided below. Never invent numbers.

            ## Rules
            - Present facts and observations only. Never give financial advice.
            - Do NOT use phrases like "consider", "you should", "why not", "try to", "we recommend".
            - Tone: warm, direct, non-judgmental, conversational.
            - Keep responses concise — aim for 2-3 sentences unless the customer asks for detail.
            - If the customer asks something not covered by the data below, say so honestly.
            - Never recommend specific investment products or third-party services.
            - This is general financial information, not personal financial advice.
            - You have a tool available to look up the customer's recent nudge history if they ask about previous insights.

            ## The Nudge
            Message: "{nudge.Message}"
            Call-to-action: "{nudge.Cta}"
            Urgency: {nudge.Urgency}
            Category: {nudge.Category}
            Reasoning: {nudge.Reasoning}
            Generated: {nudge.CreatedAt:yyyy-MM-dd HH:mm} UTC

            ## Financial Context
            Customer: {context.Customer.Name}
            Total usable balance: ${context.Financial.CurrentBalance:F2}
            Average monthly income: ${context.Financial.AvgMonthlyIncome:F2}
            Days until likely payday: {context.Financial.DaysUntilLikelyPayday}

            ### Accounts
            {accountLines}

            ### Spending This Month (last 30 days)
            {spendLines}

            ### Upcoming Payments (next 7 days)
            {(context.Upcoming.Count > 0 ? upcomingLines : "  None scheduled")}

            ### Active Signals
            {signalLines}
            """;
    }
}

public record NudgeChatSetup(string SystemPrompt, int CustomerId);
