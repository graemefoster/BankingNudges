using BankOfGraeme.Api.Models;

namespace BankOfGraeme.Api.Data;

public static class SeedData
{
    public static void Seed(BankDbContext db)
    {
        if (db.Customers.Any()) return;

        var sarah = new Customer
        {
            FirstName = "Sarah", LastName = "Mitchell",
            Email = "sarah.mitchell@email.com.au", Phone = "0412 345 678",
            DateOfBirth = new DateOnly(1988, 3, 15)
        };
        var james = new Customer
        {
            FirstName = "James", LastName = "Chen",
            Email = "james.chen@email.com.au", Phone = "0423 456 789",
            DateOfBirth = new DateOnly(1995, 7, 22)
        };
        var emma = new Customer
        {
            FirstName = "Emma", LastName = "Wilson",
            Email = "emma.wilson@email.com.au", Phone = "0434 567 890",
            DateOfBirth = new DateOnly(1991, 11, 8)
        };

        db.Customers.AddRange(sarah, james, emma);
        db.SaveChanges();

        // Sarah's accounts
        var sarahTxn = CreateAccount(sarah, AccountType.Transaction, "062-000", "10234567", "Everyday Transaction", 2450.00m);
        var sarahSav = CreateAccount(sarah, AccountType.Savings, "062-000", "10234568", "Goal Saver", 15000.00m);
        var sarahLoan = CreateAccount(sarah, AccountType.HomeLoan, "062-000", "10234569", "Home Loan", -450000.00m, 450000m, 6.2m, 360);
        var sarahOffset = CreateAccount(sarah, AccountType.Offset, "062-000", "10234570", "Offset Account", 35000.00m);

        // James's accounts
        var jamesTxn = CreateAccount(james, AccountType.Transaction, "062-000", "20345678", "Everyday Spendings", 890.00m);
        var jamesSav = CreateAccount(james, AccountType.Savings, "062-000", "20345679", "Rainy Day Fund", 8200.00m);

        // Emma's accounts
        var emmaTxn = CreateAccount(emma, AccountType.Transaction, "062-000", "30456789", "Daily Account", 5100.00m);
        var emmaLoan = CreateAccount(emma, AccountType.HomeLoan, "062-000", "30456790", "Home Loan", -320000.00m, 320000m, 5.9m, 360);
        var emmaOffset = CreateAccount(emma, AccountType.Offset, "062-000", "30456791", "Offset Savings", 12000.00m);

        db.Accounts.AddRange(sarahTxn, sarahSav, sarahLoan, sarahOffset, jamesTxn, jamesSav, emmaTxn, emmaLoan, emmaOffset);
        db.SaveChanges();

        // Update offset links (now that IDs are assigned)
        sarahOffset.HomeLoanAccountId = sarahLoan.Id;
        emmaOffset.HomeLoanAccountId = emmaLoan.Id;
        db.SaveChanges();

        // Seed transactions
        var now = DateTime.UtcNow;
        SeedTransactions(db, sarahTxn, now);
        SeedTransactions(db, sarahSav, now);
        SeedTransactions(db, jamesTxn, now);
        SeedTransactions(db, jamesSav, now);
        SeedTransactions(db, emmaTxn, now);
        SeedTransactions(db, sarahOffset, now);
        SeedTransactions(db, emmaOffset, now);
        SeedLoanTransactions(db, sarahLoan, now);
        SeedLoanTransactions(db, emmaLoan, now);

        db.SaveChanges();
    }

    private static Account CreateAccount(Customer customer, AccountType type, string bsb, string number, string name, decimal balance,
        decimal? loanAmount = null, decimal? interestRate = null, int? loanTermMonths = null)
    {
        return new Account
        {
            CustomerId = customer.Id,
            AccountType = type,
            Bsb = bsb,
            AccountNumber = number,
            Name = name,
            Balance = balance,
            LoanAmount = loanAmount,
            InterestRate = interestRate,
            LoanTermMonths = loanTermMonths
        };
    }

    private static void SeedTransactions(BankDbContext db, Account account, DateTime now)
    {
        var descriptions = account.AccountType switch
        {
            AccountType.Transaction => new[]
            {
                ("Woolworths Metro", -45.30m), ("Salary Credit", 3200.00m), ("Uber Eats", -28.50m),
                ("ATM Withdrawal", -100.00m), ("Netflix", -22.99m), ("Transfer from Savings", 500.00m),
                ("Petrol - BP", -78.20m), ("Coffee Club", -12.50m)
            },
            AccountType.Savings => new[]
            {
                ("Transfer In", 1000.00m), ("Interest Earned", 45.20m), ("Transfer Out", -500.00m),
                ("Transfer In", 2000.00m), ("Interest Earned", 52.10m)
            },
            AccountType.Offset => new[]
            {
                ("Salary Credit", 4500.00m), ("Transfer Out", -1000.00m), ("Bonus", 2500.00m),
                ("Transfer In", 1500.00m), ("Bills - Water", -180.00m)
            },
            _ => Array.Empty<(string, decimal)>()
        };

        var balance = account.Balance;
        // Work backwards from current balance to create realistic history
        var txns = new List<Transaction>();
        for (int i = descriptions.Length - 1; i >= 0; i--)
        {
            var (desc, amount) = descriptions[i];
            txns.Insert(0, new Transaction
            {
                AccountId = account.Id,
                Amount = amount,
                Description = desc,
                TransactionType = amount > 0 ? TransactionType.Deposit : TransactionType.Withdrawal,
                BalanceAfter = balance,
                CreatedAt = now.AddDays(-(descriptions.Length - i) * 2)
            });
            balance -= amount;
        }

        db.Transactions.AddRange(txns);
    }

    private static void SeedLoanTransactions(BankDbContext db, Account loan, DateTime now)
    {
        var txns = new[]
        {
            new Transaction { AccountId = loan.Id, Amount = -2325.00m, Description = "Interest Charged", TransactionType = TransactionType.Interest, BalanceAfter = loan.Balance - 2325.00m, CreatedAt = now.AddDays(-60) },
            new Transaction { AccountId = loan.Id, Amount = 3200.00m, Description = "Monthly Repayment", TransactionType = TransactionType.Repayment, BalanceAfter = loan.Balance - 2325.00m + 3200.00m, CreatedAt = now.AddDays(-58) },
            new Transaction { AccountId = loan.Id, Amount = -2310.00m, Description = "Interest Charged", TransactionType = TransactionType.Interest, BalanceAfter = loan.Balance, CreatedAt = now.AddDays(-30) },
            new Transaction { AccountId = loan.Id, Amount = 3200.00m, Description = "Monthly Repayment", TransactionType = TransactionType.Repayment, BalanceAfter = loan.Balance + 3200.00m, CreatedAt = now.AddDays(-28) },
        };

        db.Transactions.AddRange(txns);
    }
}
