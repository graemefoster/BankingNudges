namespace BankOfGraeme.Api.Services;

public enum SignalType
{
    LOW_BALANCE,
    CANT_COVER_UPCOMING,
    PAYMENT_DUE_SOON,
    SPEND_SPIKE,
    EXCESS_CASH_SITTING,
    PAYDAY_INCOMING,
    FOREIGN_SPEND_NO_HOLIDAY,
    FLIGHT_BOOKING_DETECTED
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
    private const decimal SpendSpikeMaterialityThreshold = 0.05m;

    public List<NudgeSignal> DetectSignals(
        decimal currentBalance,
        List<UpcomingPayment> upcomingPayments,
        Dictionary<string, double> spendDelta,
        Dictionary<string, decimal> spendByCategory,
        decimal avgMonthlyExpenses,
        int daysUntilPayday)
    {
        var signals = new List<NudgeSignal>();
        var totalUpcoming = upcomingPayments.Sum(p => p.Amount);
        var canComfortablyCoverUpcoming = totalUpcoming == 0 || currentBalance >= totalUpcoming * 2m;
        var balanceIsHealthy = canComfortablyCoverUpcoming
                               && avgMonthlyExpenses > 0
                               && currentBalance > avgMonthlyExpenses * 1.5m;

        if (currentBalance < LowBalanceThreshold)
        {
            signals.Add(new NudgeSignal(SignalType.LOW_BALANCE, SignalSeverity.HIGH));
        }

        if (currentBalance < totalUpcoming * 1.1m)
        {
            signals.Add(new NudgeSignal(SignalType.CANT_COVER_UPCOMING, SignalSeverity.HIGH));
        }

        // Only flag imminent payments when the balance is tight
        if (!canComfortablyCoverUpcoming)
        {
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
        }

        // Only flag spend spikes when the customer's balance is under pressure.
        // When the balance is healthy there's no reason to nag about higher spending.
        if (!balanceIsHealthy)
        {
            foreach (var (category, delta) in spendDelta)
            {
                if (delta > SpendSpikeThreshold)
                {
                    // Ignore spikes in categories where the dollar amount is immaterial
                    var minMaterialAmount = avgMonthlyExpenses * SpendSpikeMaterialityThreshold;
                    if (spendByCategory.TryGetValue(category, out var categoryAmount)
                        && categoryAmount < minMaterialAmount)
                        continue;

                    var severity = canComfortablyCoverUpcoming ? SignalSeverity.LOW : SignalSeverity.MEDIUM;
                    signals.Add(new NudgeSignal(
                        SignalType.SPEND_SPIKE,
                        severity,
                        Category: category,
                        Delta: delta));
                }
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
