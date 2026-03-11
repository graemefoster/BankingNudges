using BankOfGraeme.Api.Services;

namespace BankOfGraeme.Tests;

public class NudgeSignalDetectorTests
{
    private readonly NudgeSignalDetector _detector = new();

    [Fact]
    public void DetectsLowBalance()
    {
        var signals = _detector.DetectSignals(
            currentBalance: 350m,
            upcomingPayments: [],
            spendDelta: new Dictionary<string, double>(),
            spendByCategory: new Dictionary<string, decimal>(),
            avgMonthlyExpenses: 2000m,
            daysUntilPayday: 10);

        Assert.Contains(signals, s => s.Type == SignalType.LOW_BALANCE);
        Assert.Contains(signals, s => s.Severity == SignalSeverity.HIGH);
    }

    [Fact]
    public void DetectsCantCoverUpcoming()
    {
        var upcoming = new List<UpcomingPayment>
        {
            new("Rent", 2400m, 2, "SCHEDULED", "ScheduledPayment"),
            new("AGL", 200m, 5, "HIGH", "InferredPattern")
        };

        var signals = _detector.DetectSignals(
            currentBalance: 2500m, // less than 2600 * 1.1 = 2860
            upcomingPayments: upcoming,
            spendDelta: new Dictionary<string, double>(),
            spendByCategory: new Dictionary<string, decimal>(),
            avgMonthlyExpenses: 3000m,
            daysUntilPayday: 10);

        Assert.Contains(signals, s => s.Type == SignalType.CANT_COVER_UPCOMING);
    }

    [Fact]
    public void DetectsPaymentDueSoon()
    {
        var upcoming = new List<UpcomingPayment>
        {
            new("Rent", 2400m, 1, "SCHEDULED", "ScheduledPayment")
        };

        // Balance doesn't comfortably cover upcoming (3000 < 2400 * 2 = 4800)
        // but can still technically pay (3000 > 2400 * 1.1 = 2640)
        var signals = _detector.DetectSignals(
            currentBalance: 3000m,
            upcomingPayments: upcoming,
            spendDelta: new Dictionary<string, double>(),
            spendByCategory: new Dictionary<string, decimal>(),
            avgMonthlyExpenses: 3000m,
            daysUntilPayday: 10);

        var signal = Assert.Single(signals, s => s.Type == SignalType.PAYMENT_DUE_SOON);
        Assert.Equal("Rent", signal.PaymentMerchant);
        Assert.Equal(2400m, signal.PaymentAmount);
    }

    [Fact]
    public void DetectsSpendSpike()
    {
        var spendDelta = new Dictionary<string, double>
        {
            ["Dining"] = 0.55, // 55% increase
            ["Groceries"] = 0.10 // only 10%, below threshold
        };

        var spendByCategory = new Dictionary<string, decimal>
        {
            ["Dining"] = 250m, // material: 250 > 3000 * 0.05 = 150
            ["Groceries"] = 400m
        };

        // Balance not healthy: 2000 < 3000 * 1.5 = 4500
        var signals = _detector.DetectSignals(
            currentBalance: 2000m,
            upcomingPayments: [],
            spendDelta: spendDelta,
            spendByCategory: spendByCategory,
            avgMonthlyExpenses: 3000m,
            daysUntilPayday: 10);

        var spike = Assert.Single(signals, s => s.Type == SignalType.SPEND_SPIKE);
        Assert.Equal("Dining", spike.Category);
        Assert.Equal(0.55, spike.Delta);
    }

    [Fact]
    public void IgnoresImmaterialSpendSpike()
    {
        var spendDelta = new Dictionary<string, double>
        {
            ["Entertainment"] = 0.60 // 60% increase but tiny dollar amount
        };

        var spendByCategory = new Dictionary<string, decimal>
        {
            ["Entertainment"] = 8m // immaterial: 8 < 3000 * 0.05 = 150
        };

        // Balance not healthy so spend spikes are checked
        var signals = _detector.DetectSignals(
            currentBalance: 2000m,
            upcomingPayments: [],
            spendDelta: spendDelta,
            spendByCategory: spendByCategory,
            avgMonthlyExpenses: 3000m,
            daysUntilPayday: 10);

        Assert.DoesNotContain(signals, s => s.Type == SignalType.SPEND_SPIKE);
    }

    [Fact]
    public void DetectsExcessCash()
    {
        var signals = _detector.DetectSignals(
            currentBalance: 10000m,
            upcomingPayments: [],
            spendDelta: new Dictionary<string, double>(),
            spendByCategory: new Dictionary<string, decimal>(),
            avgMonthlyExpenses: 3000m,
            daysUntilPayday: 10);

        Assert.Contains(signals, s => s.Type == SignalType.EXCESS_CASH_SITTING);
    }

    [Fact]
    public void DetectsPaydayIncoming()
    {
        var signals = _detector.DetectSignals(
            currentBalance: 5000m,
            upcomingPayments: [],
            spendDelta: new Dictionary<string, double>(),
            spendByCategory: new Dictionary<string, decimal>(),
            avgMonthlyExpenses: 3000m,
            daysUntilPayday: 1);

        Assert.Contains(signals, s => s.Type == SignalType.PAYDAY_INCOMING);
    }

    [Fact]
    public void ReturnsEmptyWhenNoConditionsMet()
    {
        var signals = _detector.DetectSignals(
            currentBalance: 5000m,
            upcomingPayments: [],
            spendDelta: new Dictionary<string, double> { ["Groceries"] = 0.10 },
            spendByCategory: new Dictionary<string, decimal> { ["Groceries"] = 400m },
            avgMonthlyExpenses: 5000m,
            daysUntilPayday: 10);

        Assert.Empty(signals);
    }
}
