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

        var signals = _detector.DetectSignals(
            currentBalance: 10000m,
            upcomingPayments: upcoming,
            spendDelta: new Dictionary<string, double>(),
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

        var signals = _detector.DetectSignals(
            currentBalance: 5000m,
            upcomingPayments: [],
            spendDelta: spendDelta,
            avgMonthlyExpenses: 3000m,
            daysUntilPayday: 10);

        var spike = Assert.Single(signals, s => s.Type == SignalType.SPEND_SPIKE);
        Assert.Equal("Dining", spike.Category);
        Assert.Equal(0.55, spike.Delta);
    }

    [Fact]
    public void DetectsExcessCash()
    {
        var signals = _detector.DetectSignals(
            currentBalance: 10000m,
            upcomingPayments: [],
            spendDelta: new Dictionary<string, double>(),
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
            avgMonthlyExpenses: 5000m,
            daysUntilPayday: 10);

        Assert.Empty(signals);
    }
}
