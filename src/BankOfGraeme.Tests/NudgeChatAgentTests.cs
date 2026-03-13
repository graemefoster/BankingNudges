using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace BankOfGraeme.Tests;

public class NudgeChatAgentTests : IDisposable
{
    private readonly BankDbContext _db;
    private readonly NudgeChatAgent _chatAgent;
    private readonly NudgeChatTools _chatTools;

    public NudgeChatAgentTests()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dateTime = Substitute.For<IDateTimeProvider>();
        dateTime.UtcNow.Returns(new DateTime(2026, 3, 9, 12, 0, 0));
        dateTime.Today.Returns(new DateOnly(2026, 3, 9));

        _db = new BankDbContext(options, dateTime);
        _chatAgent = new NudgeChatAgent(_db, Substitute.For<ILogger<NudgeChatAgent>>());
        _chatTools = new NudgeChatTools(_db);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var customer = new Customer
        {
            Id = 1,
            FirstName = "Test",
            LastName = "Customer",
            Email = "test@example.com",
            DateOfBirth = new DateOnly(1990, 1, 1)
        };
        _db.Customers.Add(customer);

        // Nudge with a realistic ContextSnapshot
        _db.Nudges.Add(new Nudge
        {
            Id = 100,
            CustomerId = 1,
            Message = "Your dining spend is up $180 this month.",
            Cta = "See breakdown",
            Urgency = NudgeUrgency.MEDIUM,
            Category = NudgeCategory.SPENDING,
            Reasoning = "Dining category spend increased 45% vs last month",
            Status = NudgeStatus.PENDING,
            ContextSnapshot = """
            {
                "Customer": { "Id": 1, "Name": "Test Customer", "NudgeFatigueThisWeek": 1 },
                "Financial": {
                    "CurrentBalance": 2340.50,
                    "AvgMonthlyIncome": 5200.00,
                    "SpendByCategory": { "Dining": 580.00, "Groceries": 420.00, "Transport": 150.00 },
                    "SpendDelta": { "Dining": 0.45, "Groceries": -0.05, "Transport": 0.10 },
                    "DaysUntilLikelyPayday": 5,
                    "Accounts": [
                        { "Name": "Everyday", "AccountType": "Transaction", "Balance": 1840.50, "InterestRate": 0.03, "BonusInterestRate": null },
                        { "Name": "Rainy Day", "AccountType": "Savings", "Balance": 500.00, "InterestRate": 0.005, "BonusInterestRate": 0.045 }
                    ]
                },
                "Upcoming": [
                    { "Merchant": "Netflix", "Amount": 22.99, "DueInDays": 2, "Confidence": "SCHEDULED", "Source": "ScheduledPayment" }
                ],
                "Signals": [
                    { "Type": 3, "Severity": 1, "Category": "Dining", "Delta": 0.45 }
                ]
            }
            """
        });

        // Older nudge for history
        _db.Nudges.Add(new Nudge
        {
            Id = 99,
            CustomerId = 1,
            Message = "Your balance is $164.92 — you have $320 in payments due this week.",
            Cta = "View payments",
            Urgency = NudgeUrgency.HIGH,
            Category = NudgeCategory.CASHFLOW,
            Reasoning = "Low balance with upcoming payments",
            Status = NudgeStatus.ACCEPTED,
            ContextSnapshot = "{}",
            RespondedAt = new DateTime(2026, 3, 7, 10, 0, 0)
        });

        _db.SaveChanges();
    }

    [Fact]
    public async Task BuildChatSetup_ReturnsSystemPrompt_ContainingNudgeMessage()
    {
        var setup = await _chatAgent.BuildChatSetupAsync(100, 1);

        setup.ShouldNotBeNull();
        setup.SystemPrompt.ShouldContain("Your dining spend is up $180 this month.");
    }

    [Fact]
    public async Task BuildChatSetup_ReturnsSystemPrompt_ContainingFinancialData()
    {
        var setup = await _chatAgent.BuildChatSetupAsync(100, 1);

        setup.ShouldNotBeNull();
        setup.SystemPrompt.ShouldContain("2340.50"); // current balance
        setup.SystemPrompt.ShouldContain("5200.00"); // monthly income
        setup.SystemPrompt.ShouldContain("Everyday");   // account name
        setup.SystemPrompt.ShouldContain("Rainy Day");  // savings account
    }

    [Fact]
    public async Task BuildChatSetup_ReturnsSystemPrompt_ContainingRegulatoryGuardrails()
    {
        var setup = await _chatAgent.BuildChatSetupAsync(100, 1);

        setup.ShouldNotBeNull();
        setup.SystemPrompt.ShouldContain("Never give financial advice");
        setup.SystemPrompt.ShouldContain("not personal financial advice");
    }

    [Fact]
    public async Task BuildChatSetup_ReturnsSystemPrompt_ContainingUpcomingPayments()
    {
        var setup = await _chatAgent.BuildChatSetupAsync(100, 1);

        setup.ShouldNotBeNull();
        setup.SystemPrompt.ShouldContain("Netflix");
        setup.SystemPrompt.ShouldContain("$22.99");
    }

    [Fact]
    public async Task BuildChatSetup_ReturnsNull_WhenNudgeNotFound()
    {
        var setup = await _chatAgent.BuildChatSetupAsync(999, 1);
        setup.ShouldBeNull();
    }

    [Fact]
    public async Task BuildChatSetup_ReturnsNull_WhenCustomerIdDoesNotMatchNudge()
    {
        var setup = await _chatAgent.BuildChatSetupAsync(100, 999);
        setup.ShouldBeNull();
    }

    [Fact]
    public async Task GetNudgeHistory_ReturnsFormattedHistory()
    {
        var history = await _chatTools.GetNudgeHistory(1);

        history.ShouldContain("Your dining spend is up $180 this month.");
        history.ShouldContain("Your balance is $164.92");
        history.ShouldContain("SPENDING");
        history.ShouldContain("CASHFLOW");
        history.ShouldContain("Accepted");
    }

    [Fact]
    public async Task GetNudgeHistory_ReturnsNoHistory_ForUnknownCustomer()
    {
        var history = await _chatTools.GetNudgeHistory(999);
        history.ShouldContain("No previous insights found");
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
