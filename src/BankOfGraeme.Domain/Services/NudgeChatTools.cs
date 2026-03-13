using System.ComponentModel;
using System.Text;
using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Services;

/// <summary>
/// Tool functions that the nudge chat agent can invoke on demand.
/// Methods are exposed to the agent via AIFunctionFactory.Create().
/// </summary>
public class NudgeChatTools(BankDbContext db)
{
    [Description("Get the customer's recent nudge history — the last 10 financial insights they received, including the message, category, urgency, outcome (accepted/dismissed/snoozed), and when it was generated.")]
    public async Task<string> GetNudgeHistory(
        [Description("The customer's ID")] int customerId)
    {
        var nudges = await db.Nudges
            .AsNoTracking()
            .Where(n => n.CustomerId == customerId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .ToListAsync();

        if (nudges.Count == 0)
            return "No previous insights found for this customer.";

        var sb = new StringBuilder();
        sb.AppendLine($"Last {nudges.Count} insights for customer {customerId}:");
        sb.AppendLine();

        foreach (var n in nudges)
        {
            var status = n.Status switch
            {
                NudgeStatus.ACCEPTED => "✅ Accepted",
                NudgeStatus.DISMISSED => "❌ Dismissed",
                NudgeStatus.SNOOZED => "💤 Snoozed",
                NudgeStatus.EXPIRED => "⏰ Expired",
                NudgeStatus.PENDING => "⏳ Pending",
                NudgeStatus.SENT => "📤 Sent",
                _ => n.Status.ToString()
            };

            sb.AppendLine($"- [{n.CreatedAt:yyyy-MM-dd}] ({n.Category}, {n.Urgency}) {n.Message}");
            sb.AppendLine($"  Status: {status}");
            sb.AppendLine($"  Reasoning: {n.Reasoning}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
