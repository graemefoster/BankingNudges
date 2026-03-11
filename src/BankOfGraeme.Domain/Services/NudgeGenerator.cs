using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Responses;

#pragma warning disable OPENAI001 // Experimental API

namespace BankOfGraeme.Api.Services;

public record NudgeResult(
    string Message,
    string Cta,
    string Urgency,
    string Category,
    string Reasoning);

public record GenerateOutcome(NudgeResult? Nudge, string? SkipReason);

public record NudgeGeneratorSettings(string DeploymentName);

public class NudgeGenerator(
    ResponsesClient responsesClient,
    NudgeGeneratorSettings settings,
    ILogger<NudgeGenerator> logger)
{
    private const int MaxWeeklyNudges = 3;
    private string? _lastValidationFailure;

    private const string SystemPrompt = """
        You are a financial information assistant for a retail bank.

        Your job is to generate a single, timely informational nudge for this customer
        based on their financial context and active signals.

        You will receive a per-account breakdown showing each account's type, balance,
        and interest rate. Use this to make observations specific to individual accounts
        rather than just the aggregate balance. For example, note when a large balance
        sits in a low-interest Transaction account while a higher-rate Savings account
        exists.

        Rules you must follow:
        - Only use figures that appear in the context provided. Never invent numbers.
        - Be specific. Bad: "you may be overspending". Good: "your dining spend is up $180 this month".
        - Reference specific account names and balances when relevant — don't just use the total.
        - One nudge only. Pick the highest value signal if multiple exist.
        - Do NOT combine a spend spike with upcoming payments to create urgency when the customer's balance comfortably covers those payments. A spend increase is informational, not alarming, when there is plenty of cash available.
        - Do NOT describe a balance as "close to", "tight for", or "just enough for" a specific payment when the balance is more than double the payment amount. Only reference a specific payment in a low-balance context when the CANT_COVER_UPCOMING signal is active.
        - Never suggest "trimming costs" or "reviewing expenses" when the customer's balance is healthy and comfortably above their monthly spend.
        - Keep the message under 2 sentences.
        - NEVER give financial advice. Do NOT tell the customer what to do with their money. Do NOT use phrases like "consider moving", "you should", "why not", "try to", or "we recommend". Present facts and observations only; let the customer decide what action, if any, to take.
        - Tone: warm, direct, non-judgmental.
        - Never use the word "unfortunately".
        - This is contextual financial information, not personal financial advice.
        - Never recommend specific investment products or third-party services.

        Return ONLY valid JSON in this exact format, no other text:
        {
          "message": "the nudge text for the customer",
          "cta": "short label for a button that lets the customer explore further e.g. See details, View breakdown, View options",
          "urgency": "HIGH or MEDIUM or LOW",
          "category": "CASHFLOW or SAVINGS or SPENDING or UPCOMING_PAYMENT",
          "reasoning": "one sentence explaining why this nudge was chosen over others"
        }
        """;

    public async Task<GenerateOutcome> GenerateAsync(CustomerContext context)
    {
        if (context.Customer.NudgeFatigueThisWeek >= MaxWeeklyNudges)
        {
            var reason = $"nudge fatigue ({context.Customer.NudgeFatigueThisWeek} this week, max {MaxWeeklyNudges})";
            logger.LogInformation("Customer {CustomerId}: skipped — {Reason}", context.Customer.Id, reason);
            return new GenerateOutcome(null, reason);
        }

        if (context.Signals.Count == 0)
        {
            var reason = "no active signals";
            logger.LogInformation("Customer {CustomerId}: skipped — {Reason}", context.Customer.Id, reason);
            return new GenerateOutcome(null, reason);
        }

        var userPrompt = BuildUserPrompt(context);

        try
        {
            var options = new CreateResponseOptions
            {
                Instructions = SystemPrompt,
                Model = settings.DeploymentName,
                MaxOutputTokenCount = 300
            };
            options.InputItems.Add(ResponseItem.CreateUserMessageItem(userPrompt));

            logger.LogInformation("Customer {CustomerId}: calling Azure OpenAI (model={Model})",
                context.Customer.Id, settings.DeploymentName);

            var response = await responsesClient.CreateResponseAsync(options);
            var rawText = response.Value.GetOutputText();

            if (string.IsNullOrWhiteSpace(rawText))
            {
                var reason = "Azure OpenAI returned empty response";
                logger.LogError("Customer {CustomerId}: {Reason}", context.Customer.Id, reason);
                return new GenerateOutcome(null, reason);
            }

            logger.LogInformation("Customer {CustomerId}: raw LLM response: {Raw}", context.Customer.Id, rawText);

            var result = ValidateNudge(rawText, context);
            if (result is null)
            {
                return new GenerateOutcome(null, _lastValidationFailure ?? "validation failed (unknown reason)");
            }

            return new GenerateOutcome(result, null);
        }
        catch (Exception ex)
        {
            var reason = $"Azure OpenAI exception: {ex.Message}";
            logger.LogError(ex, "Customer {CustomerId}: {Reason}", context.Customer.Id, reason);
            return new GenerateOutcome(null, reason);
        }
    }

    private static string BuildUserPrompt(CustomerContext ctx)
    {
        var accountLines = string.Join("\n",
            (ctx.Financial.Accounts ?? []).Select(a =>
            {
                var rate = a.InterestRate.HasValue ? $"{a.InterestRate:F2}% p.a." : "n/a";
                var bonus = a.BonusInterestRate.HasValue ? $" + {a.BonusInterestRate:F2}% bonus" : "";
                var offsetNote = a.OffsetHomeLoanRate.HasValue
                    ? $" (offsets home loan at {a.OffsetHomeLoanRate:F2}% — this balance reduces loan interest instead of earning interest)"
                    : "";
                return $"  {a.Name} ({a.AccountType}): ${a.Balance:F2} — interest {rate}{bonus}{offsetNote}";
            }));

        var spendLines = string.Join("\n",
            ctx.Financial.SpendDelta
                .OrderByDescending(kvp => Math.Abs(kvp.Value))
                .Select(kvp => $"  {kvp.Key}: {(kvp.Value > 0 ? "+" : "")}{kvp.Value * 100:F0}%"));

        var upcomingLines = string.Join("\n",
            ctx.Upcoming.Select(p =>
                $"  {p.Merchant}: ${p.Amount:F2} due in {p.DueInDays} days (confidence: {p.Confidence})"));

        var signalLines = string.Join("\n",
            ctx.Signals.Select(s => s.Type switch
            {
                SignalType.LOW_BALANCE => $"  LOW_BALANCE: total usable balance ${ctx.Financial.CurrentBalance:F2}",
                SignalType.CANT_COVER_UPCOMING => $"  CANT_COVER_UPCOMING: total usable balance ${ctx.Financial.CurrentBalance:F2} vs upcoming ${ctx.Upcoming.Sum(p => p.Amount):F2}",
                SignalType.PAYMENT_DUE_SOON => $"  PAYMENT_DUE_SOON: {s.PaymentMerchant} ${s.PaymentAmount:F2} in {s.DueInDays} days",
                SignalType.SPEND_SPIKE => $"  SPEND_SPIKE: {s.Category} up {s.Delta * 100:F0}% vs last month",
                SignalType.EXCESS_CASH_SITTING => $"  EXCESS_CASH_SITTING: total usable balance ${ctx.Financial.CurrentBalance:F2} (see per-account breakdown above for where the money sits)",
                SignalType.PAYDAY_INCOMING => $"  PAYDAY_INCOMING: in {ctx.Financial.DaysUntilLikelyPayday} days",
                _ => $"  {s.Type}"
            }));

        return $"""
            Customer: {ctx.Customer.Name}
            Total usable balance: ${ctx.Financial.CurrentBalance:F2}
            Average monthly income: ${ctx.Financial.AvgMonthlyIncome:F2}

            Accounts:
            {accountLines}

            Spending this month vs last month:
            {spendLines}

            Upcoming payments in next 7 days:
            {upcomingLines}

            Active signals:
            {signalLines}

            Generate a nudge for this customer.
            """;
    }

    internal NudgeResult? ValidateNudge(string rawJson, CustomerContext context)
    {
        _lastValidationFailure = null;
        NudgeResponse? parsed;
        try
        {
            var jsonText = ExtractJson(rawJson);
            parsed = JsonSerializer.Deserialize<NudgeResponse>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _lastValidationFailure = $"JSON parse failed: {ex.Message}. Raw: {rawJson[..Math.Min(200, rawJson.Length)]}";
            logger.LogError(ex, "Failed to parse nudge JSON for customer {CustomerId}. Raw: {Raw}",
                context.Customer.Id, rawJson);
            return null;
        }

        if (parsed is null ||
            string.IsNullOrWhiteSpace(parsed.Message) ||
            string.IsNullOrWhiteSpace(parsed.Cta) ||
            string.IsNullOrWhiteSpace(parsed.Reasoning))
        {
            _lastValidationFailure = $"missing required fields — message={parsed?.Message != null}, cta={parsed?.Cta != null}, reasoning={parsed?.Reasoning != null}";
            logger.LogWarning("Customer {CustomerId}: {Failure}", context.Customer.Id, _lastValidationFailure);
            return null;
        }

        // Hallucination check — extract all dollar amounts from message
        var numbersInMessage = Regex.Matches(parsed.Message, @"\$([\d,]+\.?\d*)")
            .Select(m => decimal.TryParse(m.Groups[1].Value.Replace(",", ""), out var v) ? v : (decimal?)null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        var contextNumbers = BuildContextNumbers(context);
        foreach (var num in numbersInMessage)
        {
            if (!contextNumbers.Any(cn => Math.Abs(cn - num) < 1.0m))
            {
                _lastValidationFailure = $"hallucination: ${num} not found in context numbers [{string.Join(", ", contextNumbers.Select(n => $"${n}"))}]. Message: \"{parsed.Message}\"";
                logger.LogWarning("Customer {CustomerId}: {Failure}", context.Customer.Id, _lastValidationFailure);
                return null;
            }
        }

        // Length check
        var wordCount = parsed.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount > 40)
        {
            _lastValidationFailure = $"message too long ({wordCount} words, max 40): \"{parsed.Message[..Math.Min(100, parsed.Message.Length)]}...\"";
            logger.LogWarning("Customer {CustomerId}: {Failure}", context.Customer.Id, _lastValidationFailure);
            return null;
        }

        // Validate urgency
        var urgency = parsed.Urgency?.ToUpperInvariant() switch
        {
            "HIGH" => "HIGH",
            "LOW" => "LOW",
            _ => "MEDIUM"
        };

        // Validate category
        var category = parsed.Category?.ToUpperInvariant() switch
        {
            "CASHFLOW" => "CASHFLOW",
            "SAVINGS" => "SAVINGS",
            "SPENDING" => "SPENDING",
            "UPCOMING_PAYMENT" => "UPCOMING_PAYMENT",
            _ => "CASHFLOW"
        };

        return new NudgeResult(
            Message: parsed.Message,
            Cta: parsed.Cta,
            Urgency: urgency,
            Category: category,
            Reasoning: parsed.Reasoning);
    }

    private static HashSet<decimal> BuildContextNumbers(CustomerContext ctx)
    {
        var numbers = new HashSet<decimal>
        {
            Math.Round(ctx.Financial.CurrentBalance, 2),
            Math.Round(ctx.Financial.AvgMonthlyIncome, 2)
        };

        // Add per-account balances so the LLM can reference individual accounts
        if (ctx.Financial.Accounts is not null)
        {
            foreach (var account in ctx.Financial.Accounts)
            {
                numbers.Add(Math.Round(account.Balance, 2));
            }
        }

        foreach (var (_, amount) in ctx.Financial.SpendByCategory)
        {
            numbers.Add(Math.Round(amount, 2));
        }

        foreach (var payment in ctx.Upcoming)
        {
            numbers.Add(Math.Round(payment.Amount, 2));
        }

        // Add totals that the LLM might compute
        var totalUpcoming = ctx.Upcoming.Sum(p => p.Amount);
        numbers.Add(Math.Round(totalUpcoming, 2));

        // Add spend delta percentages as absolute amounts
        foreach (var (category, delta) in ctx.Financial.SpendDelta)
        {
            if (ctx.Financial.SpendByCategory.TryGetValue(category, out var catAmount))
            {
                var priorAmount = catAmount / (1 + (decimal)delta);
                var diff = catAmount - priorAmount;
                numbers.Add(Math.Round(Math.Abs(diff), 2));
                numbers.Add(Math.Round(Math.Abs(diff), 0));
            }
            numbers.Add(Math.Round((decimal)(Math.Abs(delta) * 100), 0));
        }

        return numbers;
    }

    private static string ExtractJson(string text)
    {
        // Strip markdown code fences if present
        var match = Regex.Match(text, @"```(?:json)?\s*(\{.*?\})\s*```", RegexOptions.Singleline);
        if (match.Success) return match.Groups[1].Value;

        // Try to find raw JSON object
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
            return text[start..(end + 1)];

        return text;
    }

    private record NudgeResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; init; }

        [JsonPropertyName("cta")]
        public string? Cta { get; init; }

        [JsonPropertyName("urgency")]
        public string? Urgency { get; init; }

        [JsonPropertyName("category")]
        public string? Category { get; init; }

        [JsonPropertyName("reasoning")]
        public string? Reasoning { get; init; }
    }
}
