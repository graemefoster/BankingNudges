namespace BankOfGraeme.Api.Data;

/// <summary>
/// Pure calculation helpers for foreign currency transactions.
/// Extracted from SeedData to enable unit testing.
/// </summary>
internal static class ForeignTransactionCalculator
{
    /// <summary>
    /// Convert a foreign currency amount to AUD using the given exchange rate.
    /// Rate is expressed as units of foreign currency per 1 AUD.
    /// </summary>
    public static decimal ConvertToAud(decimal foreignAmount, decimal exchangeRate)
        => Math.Round(foreignAmount / exchangeRate, 2);

    /// <summary>
    /// Calculate the international transaction fee as a percentage of the AUD amount.
    /// </summary>
    public static decimal CalculateFee(decimal audAmount, decimal feeRate)
        => Math.Round(audAmount * feeRate, 2);

    /// <summary>
    /// Apply a daily variance to a base exchange rate (±percentage).
    /// </summary>
    public static decimal ApplyFxVariance(decimal baseRate, double varianceFactor)
        => Math.Round(baseRate * (1m + (decimal)varianceFactor), 6);
}
