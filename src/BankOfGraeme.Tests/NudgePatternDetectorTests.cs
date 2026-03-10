using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;

namespace BankOfGraeme.Tests;

public class NudgePatternDetectorTests
{
    private readonly NudgePatternDetector _detector = new();

    [Fact]
    public void DetectsExactMonthlyPayments()
    {
        var transactions = CreateMonthlyTransactions("RENT PAYMENT", -2400m, 6, exactDays: true);

        var patterns = _detector.DetectRecurringPatterns(transactions);

        var pattern = Assert.Single(patterns);
        Assert.Equal(RecurringFrequency.MONTHLY, pattern.Frequency);
        Assert.Equal(2400m, pattern.AvgAmount);
        Assert.True(pattern.IsExactAmount);
        Assert.Equal(PatternConfidence.HIGH, pattern.Confidence);
    }

    [Fact]
    public void DetectsRoughlyMonthlyPayments()
    {
        // Payments that vary by ±3 days
        var baseDate = new DateTime(2026, 1, 15);
        var transactions = new List<Transaction>
        {
            MakeTxn("RENT PAYMENT", -2400m, baseDate),
            MakeTxn("RENT PAYMENT", -2400m, baseDate.AddDays(28)),
            MakeTxn("RENT PAYMENT", -2400m, baseDate.AddDays(59)),
            MakeTxn("RENT PAYMENT", -2400m, baseDate.AddDays(90)),
            MakeTxn("RENT PAYMENT", -2400m, baseDate.AddDays(121)),
        };

        var patterns = _detector.DetectRecurringPatterns(transactions);

        var pattern = Assert.Single(patterns);
        Assert.Equal(RecurringFrequency.MONTHLY, pattern.Frequency);
    }

    [Fact]
    public void DetectsWeeklyPayments()
    {
        var transactions = CreateWeeklyTransactions("COFFEE BEAN", -5.50m, 8);

        var patterns = _detector.DetectRecurringPatterns(transactions);

        var pattern = Assert.Single(patterns);
        Assert.Equal(RecurringFrequency.WEEKLY, pattern.Frequency);
    }

    [Fact]
    public void SkipsIrregularTransactions()
    {
        var baseDate = new DateTime(2026, 1, 1);
        var transactions = new List<Transaction>
        {
            MakeTxn("RANDOM SHOP", -50m, baseDate),
            MakeTxn("RANDOM SHOP", -30m, baseDate.AddDays(3)),
            MakeTxn("RANDOM SHOP", -45m, baseDate.AddDays(47)),
            MakeTxn("RANDOM SHOP", -20m, baseDate.AddDays(52)),
        };

        var patterns = _detector.DetectRecurringPatterns(transactions);

        // High gap variance should exclude this
        Assert.Empty(patterns);
    }

    [Fact]
    public void SkipsMerchantsWithFewerThan3Transactions()
    {
        var baseDate = new DateTime(2026, 1, 1);
        var transactions = new List<Transaction>
        {
            MakeTxn("ONE-OFF SHOP", -100m, baseDate),
            MakeTxn("ONE-OFF SHOP", -100m, baseDate.AddDays(30)),
        };

        var patterns = _detector.DetectRecurringPatterns(transactions);

        Assert.Empty(patterns);
    }

    [Fact]
    public void IgnoresCreditTransactions()
    {
        var transactions = CreateMonthlyTransactions("SALARY CREDIT", 5000m, 6, exactDays: true);

        var patterns = _detector.DetectRecurringPatterns(transactions);

        Assert.Empty(patterns);
    }

    private static List<Transaction> CreateMonthlyTransactions(string desc, decimal amount, int count, bool exactDays)
    {
        var baseDate = new DateTime(2025, 7, 1);
        return Enumerable.Range(0, count)
            .Select(i => MakeTxn(desc, amount, baseDate.AddDays(i * 30)))
            .ToList();
    }

    private static List<Transaction> CreateWeeklyTransactions(string desc, decimal amount, int count)
    {
        var baseDate = new DateTime(2025, 10, 1);
        return Enumerable.Range(0, count)
            .Select(i => MakeTxn(desc, amount, baseDate.AddDays(i * 7)))
            .ToList();
    }

    private static Transaction MakeTxn(string desc, decimal amount, DateTime date) => new()
    {
        Id = 0,
        AccountId = 1,
        Description = desc,
        Amount = amount,
        TransactionType = amount > 0 ? TransactionType.Deposit : TransactionType.Withdrawal,
        Status = TransactionStatus.Settled,
        CreatedAt = date
    };
}
