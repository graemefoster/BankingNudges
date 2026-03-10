using BankOfGraeme.Api.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace BankOfGraeme.Tests;

public class NudgeGeneratorValidationTests
{
    private readonly NudgeGenerator _generator;

    public NudgeGeneratorValidationTests()
    {
        var responsesClient = Substitute.For<ResponsesClient>();
        var logger = Substitute.For<ILogger<NudgeGenerator>>();
        _generator = new NudgeGenerator(responsesClient, new NudgeGeneratorSettings("test-model"), logger);
    }

    private static CustomerContext MakeContext(decimal balance = 1840m, decimal income = 5000m) => new(
        Customer: new CustomerInfo(1, "Test Customer", 0),
        Financial: new FinancialInfo(
            CurrentBalance: balance,
            AvgMonthlyIncome: income,
            SpendByCategory: new Dictionary<string, decimal>
            {
                ["Dining"] = 580m,
                ["Groceries"] = 420m
            },
            SpendDelta: new Dictionary<string, double>
            {
                ["Dining"] = 0.45,
                ["Groceries"] = -0.05
            },
            DaysUntilLikelyPayday: 5),
        Upcoming: [new UpcomingPayment("Eastside Properties", 2400m, 2, "SCHEDULED", "ScheduledPayment")],
        Signals: [new NudgeSignal(SignalType.CANT_COVER_UPCOMING, SignalSeverity.HIGH)]);

    [Fact]
    public void ValidNudge_ParsesSuccessfully()
    {
        var json = """
        {
          "message": "Your rent payment to Eastside Properties ($2,400) is due in 2 days and your current balance is $1,840. Move funds to cover it.",
          "cta": "Move funds",
          "urgency": "HIGH",
          "category": "UPCOMING_PAYMENT",
          "reasoning": "Upcoming payment exceeds balance"
        }
        """;

        var result = _generator.ValidateNudge(json, MakeContext());

        Assert.NotNull(result);
        Assert.Equal("Move funds", result.Cta);
        Assert.Equal("HIGH", result.Urgency);
        Assert.Equal("UPCOMING_PAYMENT", result.Category);
    }

    [Fact]
    public void RejectsHallucinatedNumbers()
    {
        var json = """
        {
          "message": "Your balance is $9,999 which is critically low.",
          "cta": "Review now",
          "urgency": "HIGH",
          "category": "CASHFLOW",
          "reasoning": "Made up number"
        }
        """;

        var result = _generator.ValidateNudge(json, MakeContext());

        Assert.Null(result);
    }

    [Fact]
    public void RejectsInvalidJson()
    {
        var result = _generator.ValidateNudge("not json at all", MakeContext());
        Assert.Null(result);
    }

    [Fact]
    public void RejectsMissingFields()
    {
        var json = """
        {
          "message": "Some nudge",
          "cta": "Do it"
        }
        """;

        var result = _generator.ValidateNudge(json, MakeContext());
        Assert.Null(result);
    }

    [Fact]
    public void DefaultsInvalidUrgencyToMedium()
    {
        var json = """
        {
          "message": "Your balance is $1,840.",
          "cta": "Review",
          "urgency": "SUPER_HIGH",
          "category": "CASHFLOW",
          "reasoning": "Testing defaults"
        }
        """;

        var result = _generator.ValidateNudge(json, MakeContext());

        Assert.NotNull(result);
        Assert.Equal("MEDIUM", result.Urgency);
    }

    [Fact]
    public void HandlesMarkdownCodeBlocks()
    {
        var json = """
        ```json
        {
          "message": "Your balance is $1,840.",
          "cta": "Review",
          "urgency": "HIGH",
          "category": "CASHFLOW",
          "reasoning": "Balance check"
        }
        ```
        """;

        var result = _generator.ValidateNudge(json, MakeContext());

        Assert.NotNull(result);
        Assert.Equal("HIGH", result.Urgency);
    }

    [Fact]
    public void RejectsOverlyLongMessages()
    {
        var longMessage = string.Join(" ", Enumerable.Repeat("word", 50));
        var json = $$"""
        {
          "message": "{{longMessage}}",
          "cta": "Review",
          "urgency": "HIGH",
          "category": "CASHFLOW",
          "reasoning": "Too long"
        }
        """;

        var result = _generator.ValidateNudge(json, MakeContext());

        Assert.Null(result);
    }
}
