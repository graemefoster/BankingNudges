using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;

namespace BankOfGraeme.Api.Services.InterestCalculation;

/// <summary>
/// Calculates daily interest earned on transaction account positive balances.
/// </summary>
public class TransactionAccountInterestCalculator : IAccountInterestCalculator
{
    public AccountType AccountType => AccountType.Transaction;

    public IQueryable<Account> GetEligibleAccounts(BankDbContext db)
    {
        return db.Accounts
            .Where(a => a.IsActive && a.AccountType == AccountType.Transaction && a.InterestRate != null);
    }

    public decimal CalculateDailyInterest(Account account)
    {
        var balance = Math.Max(0, account.Balance);
        if (balance == 0) return 0;

        var dailyRate = account.InterestRate!.Value / 100m / 365m;
        return balance * dailyRate;
    }
}
