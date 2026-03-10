using BankOfGraeme.Api.Models;

namespace BankOfGraeme.Api.Services;

public enum RecurringFrequency
{
    WEEKLY,
    FORTNIGHTLY,
    MONTHLY,
    IRREGULAR
}

public enum PatternConfidence
{
    LOW,
    MEDIUM,
    HIGH
}

public record RecurringPattern(
    string Merchant,
    RecurringFrequency Frequency,
    decimal AvgAmount,
    decimal AmountVariance,
    DateTime LastPaidDate,
    DateTime NextExpectedDate,
    PatternConfidence Confidence,
    bool IsExactAmount);

public class NudgePatternDetector
{
    public List<RecurringPattern> DetectRecurringPatterns(List<Transaction> transactions)
    {
        var patterns = new List<RecurringPattern>();

        var grouped = transactions
            .Where(t => t.Amount < 0) // only debits
            .GroupBy(t => NormaliseMerchant(t.Description));

        foreach (var group in grouped)
        {
            var txns = group.OrderBy(t => t.CreatedAt).ToList();
            if (txns.Count < 3) continue;

            var gaps = new List<double>();
            for (var i = 1; i < txns.Count; i++)
            {
                gaps.Add((txns[i].CreatedAt - txns[i - 1].CreatedAt).TotalDays);
            }

            var avgGap = gaps.Average();
            var gapVariance = StandardDeviation(gaps);

            var amounts = txns.Select(t => Math.Abs(t.Amount)).ToList();
            var avgAmount = amounts.Average();
            var amountVariance = StandardDeviation(amounts.Select(a => (double)a).ToList());

            if (gapVariance >= 5) continue;

            var frequency = ClassifyFrequency(avgGap);
            var confidence = ClassifyConfidence(txns.Count, gapVariance);
            var lastPaid = txns[^1].CreatedAt;
            var nextExpected = lastPaid.AddDays(avgGap);

            patterns.Add(new RecurringPattern(
                Merchant: group.Key,
                Frequency: frequency,
                AvgAmount: Math.Round(avgAmount, 2),
                AmountVariance: Math.Round((decimal)amountVariance, 2),
                LastPaidDate: lastPaid,
                NextExpectedDate: nextExpected,
                Confidence: confidence,
                IsExactAmount: amountVariance < 1.0));
        }

        return patterns;
    }

    private static RecurringFrequency ClassifyFrequency(double avgGap) => avgGap switch
    {
        <= 9 => RecurringFrequency.WEEKLY,
        <= 17 => RecurringFrequency.FORTNIGHTLY,
        <= 24 => RecurringFrequency.FORTNIGHTLY, // ~3 weekly cycles, close enough to fortnightly
        <= 35 => RecurringFrequency.MONTHLY,
        _ => RecurringFrequency.IRREGULAR
    };

    private static PatternConfidence ClassifyConfidence(int occurrences, double gapVariance) =>
        (occurrences, gapVariance) switch
        {
            ( >= 6, < 2) => PatternConfidence.HIGH,
            ( >= 4, < 3) => PatternConfidence.HIGH,
            ( >= 4, _) => PatternConfidence.MEDIUM,
            ( >= 3, < 3) => PatternConfidence.MEDIUM,
            _ => PatternConfidence.LOW
        };

    private static string NormaliseMerchant(string description)
    {
        // Strip common prefixes and normalise for grouping
        var normalised = description
            .Replace("DIRECT DEBIT - ", "")
            .Replace("CARD PURCHASE ", "")
            .Replace("OSKO PAYMENT - ", "")
            .Trim()
            .ToUpperInvariant();
        return normalised;
    }

    private static double StandardDeviation(IList<double> values)
    {
        if (values.Count <= 1) return 0;
        var avg = values.Average();
        var sumSquares = values.Sum(v => (v - avg) * (v - avg));
        return Math.Sqrt(sumSquares / (values.Count - 1));
    }

    private static double StandardDeviation(List<double> values) =>
        StandardDeviation((IList<double>)values);
}
