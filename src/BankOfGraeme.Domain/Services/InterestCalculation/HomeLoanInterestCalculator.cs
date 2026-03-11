using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Services.InterestCalculation;

/// <summary>
/// Calculates daily interest for home loan accounts using reducing-balance
/// amortization. Offset account balances reduce the interest-bearing principal.
/// </summary>
public class HomeLoanInterestCalculator : IAccountInterestCalculator
{
    public AccountType AccountType => AccountType.HomeLoan;

    public IQueryable<Account> GetEligibleAccounts(BankDbContext db)
    {
        return db.Accounts
            .Include(a => a.OffsetAccounts)
            .Where(a => a.IsActive && a.AccountType == AccountType.HomeLoan && a.InterestRate != null);
    }

    public decimal CalculateDailyInterest(Account loan)
    {
        var principal = Math.Max(0, -loan.Balance);
        var offsetBalance = loan.OffsetAccounts
            .Where(o => o.IsActive)
            .Sum(o => Math.Max(0, o.Balance));

        return CalculateDailyInterest(principal, offsetBalance, loan.InterestRate!.Value);
    }

    /// <summary>
    /// Calculate the daily interest for given principal, offset, and rate values.
    /// Shared by both the real nightly accrual and seed data generation to guarantee consistency.
    /// </summary>
    /// <returns>Negative value representing interest charged (increases debt). Zero when no interest applies.</returns>
    public static decimal CalculateDailyInterest(decimal principal, decimal offsetBalance, decimal annualRatePercent)
    {
        if (principal <= 0) return 0;

        var effectivePrincipal = Math.Max(0, principal - offsetBalance);
        var dailyRate = annualRatePercent / 100m / 365m;

        return -(effectivePrincipal * dailyRate);
    }

    /// <summary>
    /// Calculate the fixed monthly repayment for an amortized home loan using the PMT formula:
    /// M = P × r(1+r)^n / ((1+r)^n - 1)
    /// </summary>
    /// <param name="principal">Remaining loan principal (positive value).</param>
    /// <param name="annualRatePercent">Annual interest rate as a percentage (e.g. 5.5 for 5.5%).</param>
    /// <param name="remainingTermMonths">Number of months remaining on the loan.</param>
    /// <param name="offsetBalance">Total balance across linked offset accounts (reduces interest-bearing principal).</param>
    /// <returns>The fixed monthly repayment amount.</returns>
    public static decimal CalculateMonthlyRepayment(
        decimal principal,
        decimal annualRatePercent,
        int remainingTermMonths,
        decimal offsetBalance = 0)
    {
        if (remainingTermMonths <= 0) return 0;

        var effectivePrincipal = Math.Max(0, principal - offsetBalance);
        if (effectivePrincipal == 0) return 0;

        var monthlyRate = annualRatePercent / 100m / 12m;

        if (monthlyRate == 0)
            return effectivePrincipal / remainingTermMonths;

        // PMT = P × r(1+r)^n / ((1+r)^n - 1)
        var onePlusR = (double)(1m + monthlyRate);
        var power = Math.Pow(onePlusR, remainingTermMonths);
        var payment = (double)effectivePrincipal * (double)monthlyRate * power / (power - 1);

        return Math.Round((decimal)payment, 2);
    }
}
