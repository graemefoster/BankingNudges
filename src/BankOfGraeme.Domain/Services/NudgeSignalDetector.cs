namespace BankOfGraeme.Api.Services;

public enum SignalType
{
    LOW_BALANCE,
    CANT_COVER_UPCOMING,
    PAYMENT_DUE_SOON,
    SPEND_SPIKE,
    EXCESS_CASH_SITTING,
    PAYDAY_INCOMING
}

public enum SignalSeverity
{
    LOW,
    MEDIUM,
    HIGH
}

public record NudgeSignal(
    SignalType Type,
    SignalSeverity Severity,
    string? Category = null,
    double? Delta = null,
    string? PaymentMerchant = null,
    decimal? PaymentAmount = null,
    int? DueInDays = null);

public class NudgeSignalDetector
{
    private const decimal LowBalanceThreshold = 500m;
    private const double SpendSpikeThreshold = 0.40;

    public List<NudgeSignal> DetectSignals(
        decimal currentBalance,
        List<UpcomingPayment> upcomingPayments,
        Dictionary<string, double> spendDelta,
        decimal avgMonthlyExpenses,
        int daysUntilPayday)
    {
        var signals = new List<NudgeSignal>();

        if (currentBalance < LowBalanceThreshold)
        {
            signals.Add(new NudgeSignal(SignalType.LOW_BALANCE, SignalSeverity.HIGH));
        }

        var totalUpcoming = upcomingPayments.Sum(p => p.Amount);
        if (currentBalance < totalUpcoming * 1.1m)
        {
            signals.Add(new NudgeSignal(SignalType.CANT_COVER_UPCOMING, SignalSeverity.HIGH));
        }

        foreach (var payment in upcomingPayments)
        {
            if (payment.DueInDays <= 2)
            {
                signals.Add(new NudgeSignal(
                    SignalType.PAYMENT_DUE_SOON,
                    SignalSeverity.MEDIUM,
                    PaymentMerchant: payment.Merchant,
                    PaymentAmount: payment.Amount,
                    DueInDays: payment.DueInDays));
            }
        }

        foreach (var (category, delta) in spendDelta)
        {
            if (delta > SpendSpikeThreshold)
            {
                signals.Add(new NudgeSignal(
                    SignalType.SPEND_SPIKE,
                    SignalSeverity.MEDIUM,
                    Category: category,
                    Delta: delta));
            }
        }

        if (avgMonthlyExpenses > 0 && currentBalance > avgMonthlyExpenses * 1.5m)
        {
            signals.Add(new NudgeSignal(SignalType.EXCESS_CASH_SITTING, SignalSeverity.LOW));
        }

        if (daysUntilPayday <= 1)
        {
            signals.Add(new NudgeSignal(SignalType.PAYDAY_INCOMING, SignalSeverity.LOW));
        }

        return signals;
    }
}

public record UpcomingPayment(
    string Merchant,
    decimal Amount,
    int DueInDays,
    string Confidence,
    string Source);
