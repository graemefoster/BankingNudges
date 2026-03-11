using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BankOfGraeme.Api.Services.InterestCalculation;

/// <summary>
/// Strategy interface for per-account-type daily interest calculation.
/// </summary>
public interface IAccountInterestCalculator
{
    AccountType AccountType { get; }

    /// <summary>
    /// Returns a queryable for all accounts eligible for daily interest accrual.
    /// Implementations should include any related data needed for calculation (e.g. offset accounts).
    /// </summary>
    IQueryable<Account> GetEligibleAccounts(BankDbContext db);

    /// <summary>
    /// Calculate the daily interest amount for a single account.
    /// Positive = interest earned, negative = interest charged.
    /// Returns zero when no interest should accrue.
    /// </summary>
    decimal CalculateDailyInterest(Account account);
}
