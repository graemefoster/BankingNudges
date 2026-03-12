using BankOfGraeme.Api.Data;
using Shouldly;

namespace BankOfGraeme.Tests;

public class ForeignTransactionCalculatorTests
{
    [Theory]
    [InlineData(100_000, 10_300, 9.71)]     // 100,000 IDR → ~$9.71 AUD
    [InlineData(1_000, 23.5, 42.55)]         // 1,000 THB → ~$42.55 AUD
    [InlineData(100, 0.65, 153.85)]           // 100 USD → ~$153.85 AUD
    [InlineData(10_000, 97.0, 103.09)]        // 10,000 JPY → ~$103.09 AUD
    [InlineData(50, 0.52, 96.15)]             // 50 GBP → ~$96.15 AUD
    public void ConvertToAud_converts_foreign_to_aud_using_rate(
        double foreignAmount, double exchangeRate, double expectedAud)
    {
        var result = ForeignTransactionCalculator.ConvertToAud(
            (decimal)foreignAmount, (decimal)exchangeRate);

        result.ShouldBe((decimal)expectedAud, tolerance: 0.01m);
    }

    [Theory]
    [InlineData(100.00, 0.03, 3.00)]   // 3% of $100
    [InlineData(42.55, 0.03, 1.28)]    // 3% of $42.55
    [InlineData(9.71, 0.03, 0.29)]     // 3% of $9.71
    [InlineData(0, 0.03, 0)]           // Zero amount
    public void CalculateFee_returns_percentage_of_aud(
        double audAmount, double feeRate, double expectedFee)
    {
        var result = ForeignTransactionCalculator.CalculateFee(
            (decimal)audAmount, (decimal)feeRate);

        result.ShouldBe((decimal)expectedFee, tolerance: 0.01m);
    }

    [Fact]
    public void ApplyFxVariance_with_zero_variance_returns_base_rate()
    {
        var baseRate = 10_300m;
        var result = ForeignTransactionCalculator.ApplyFxVariance(baseRate, 0.0);

        result.ShouldBe(baseRate);
    }

    [Fact]
    public void ApplyFxVariance_with_positive_2_percent_increases_rate()
    {
        var baseRate = 10_300m;
        var result = ForeignTransactionCalculator.ApplyFxVariance(baseRate, 0.02);

        result.ShouldBe(10_506m, tolerance: 1m);
    }

    [Fact]
    public void ApplyFxVariance_with_negative_2_percent_decreases_rate()
    {
        var baseRate = 10_300m;
        var result = ForeignTransactionCalculator.ApplyFxVariance(baseRate, -0.02);

        result.ShouldBe(10_094m, tolerance: 1m);
    }

    [Fact]
    public void Full_conversion_pipeline_bali_dinner()
    {
        // Simulate a 150,000 IDR dinner in Bali
        var foreignAmount = 150_000m;
        var baseRate = 10_300m;
        var feeRate = 0.03m;

        var effectiveRate = ForeignTransactionCalculator.ApplyFxVariance(baseRate, 0.01); // +1% day
        var audAmount = ForeignTransactionCalculator.ConvertToAud(foreignAmount, effectiveRate);
        var fee = ForeignTransactionCalculator.CalculateFee(audAmount, feeRate);
        var totalDebit = audAmount + fee;

        // Base: 150,000 / 10,300 ≈ $14.56 AUD
        // With +1% rate: 150,000 / 10,403 ≈ $14.42 AUD (higher rate = cheaper)
        audAmount.ShouldBeInRange(13m, 16m);
        fee.ShouldBeGreaterThan(0m);
        fee.ShouldBeLessThan(1m);
        totalDebit.ShouldBeGreaterThan(audAmount);
    }

    [Fact]
    public void Full_conversion_pipeline_europe_shopping()
    {
        // Simulate a €80 purchase in Europe
        var foreignAmount = 80m;
        var baseRate = 0.61m;
        var feeRate = 0.03m;

        var effectiveRate = ForeignTransactionCalculator.ApplyFxVariance(baseRate, -0.005); // -0.5% day
        var audAmount = ForeignTransactionCalculator.ConvertToAud(foreignAmount, effectiveRate);
        var fee = ForeignTransactionCalculator.CalculateFee(audAmount, feeRate);
        var totalDebit = audAmount + fee;

        // Base: 80 / 0.61 ≈ $131.15 AUD
        audAmount.ShouldBeInRange(125m, 140m);
        fee.ShouldBeInRange(3m, 5m);
        totalDebit.ShouldBe(audAmount + fee);
    }
}
