using Microsoft.EntityFrameworkCore;
using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;

namespace BankOfGraeme.Api.Services;

public class AccountService(BankDbContext db, IDateTimeProvider dateTime)
{
    private const int MaxRetries = 3;

    public async Task<Account?> GetAccountAsync(int id) =>
        await db.Accounts.Include(a => a.OffsetAccounts).FirstOrDefaultAsync(a => a.Id == id);

    public async Task<List<Account>> GetCustomerAccountsAsync(int customerId, bool includeInactive = false) =>
        await db.Accounts
            .Where(a => a.CustomerId == customerId && (includeInactive || a.IsActive))
            .OrderBy(a => a.AccountType)
            .ThenBy(a => a.Id)
            .ToListAsync();

    /// <summary>
    /// Available balance = ledger balance minus pending holds (unsigned pending withdrawal amounts).
    /// </summary>
    public async Task<decimal> GetAvailableBalanceAsync(int accountId)
    {
        var account = await db.Accounts.FindAsync(accountId)
            ?? throw new InvalidOperationException("Account not found");

        var pendingHolds = await db.Transactions
            .Where(t => t.AccountId == accountId && t.Status == TransactionStatus.Pending && t.Amount < 0)
            .SumAsync(t => t.Amount); // negative sum

        return account.Balance + pendingHolds; // Balance - abs(pending debits)
    }

    public async Task<Transaction> DepositAsync(int accountId, decimal amount, string description)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Deposit amount must be positive");

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var account = await db.Accounts.FindAsync(accountId)
                ?? throw new InvalidOperationException("Account not found");

            if (account.AccountType == AccountType.HomeLoan)
                throw new InvalidOperationException("Cannot deposit to a home loan account. Use repayment instead.");

            // Deposits settle immediately
            account.Balance += amount;

            var txn = new Transaction
            {
                AccountId = accountId,
                Amount = amount,
                Description = description,
                TransactionType = TransactionType.Deposit,
                Status = TransactionStatus.Settled,
                SettledAt = dateTime.UtcNow,
            };

            db.Transactions.Add(txn);

            try
            {
                await db.SaveChangesAsync();
                return txn;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxRetries - 1)
            {
                db.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException("Unable to complete deposit due to concurrent access. Please try again.");
    }

    public async Task<Transaction> WithdrawAsync(int accountId, decimal amount, string description)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Withdrawal amount must be positive");

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var account = await db.Accounts.FindAsync(accountId)
                ?? throw new InvalidOperationException("Account not found");

            if (account.AccountType == AccountType.HomeLoan)
                throw new InvalidOperationException("Cannot withdraw from a home loan account");

            // Check available balance (ledger minus pending holds)
            var pendingHolds = await db.Transactions
                .Where(t => t.AccountId == accountId && t.Status == TransactionStatus.Pending && t.Amount < 0)
                .SumAsync(t => t.Amount);
            var availableBalance = account.Balance + pendingHolds;

            if (availableBalance < amount)
                throw new InvalidOperationException("Insufficient funds");

            // Force a RowVersion check to prevent concurrent overdraw.
            // Pending withdrawals don't change ledger balance, but touching
            // the entity ensures optimistic concurrency catches races.
            db.Entry(account).State = EntityState.Modified;

            // Card-like withdrawals are pending; they don't touch the ledger yet
            var txn = new Transaction
            {
                AccountId = accountId,
                Amount = -amount,
                Description = description,
                TransactionType = TransactionType.Withdrawal,
                Status = TransactionStatus.Pending,
            };

            db.Transactions.Add(txn);

            try
            {
                await db.SaveChangesAsync();
                return txn;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxRetries - 1)
            {
                db.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException("Unable to complete withdrawal due to concurrent access. Please try again.");
    }

    public async Task<(Transaction from, Transaction to)> TransferAsync(int fromAccountId, int toAccountId, decimal amount, string? description = null)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Transfer amount must be positive");

        if (fromAccountId == toAccountId)
            throw new InvalidOperationException("Cannot transfer to the same account");

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var fromAccount = await db.Accounts.FindAsync(fromAccountId)
                ?? throw new InvalidOperationException("Source account not found");
            var toAccount = await db.Accounts.FindAsync(toAccountId)
                ?? throw new InvalidOperationException("Destination account not found");

            if (fromAccount.CustomerId != toAccount.CustomerId)
                throw new InvalidOperationException("Can only transfer between your own accounts");

            if (fromAccount.AccountType == AccountType.HomeLoan)
                throw new InvalidOperationException("Cannot transfer from a home loan account");

            if (toAccount.AccountType == AccountType.HomeLoan)
                throw new InvalidOperationException("Cannot transfer to a home loan. Use repayment instead.");

            var pendingHolds = await db.Transactions
                .Where(t => t.AccountId == fromAccountId && t.Status == TransactionStatus.Pending && t.Amount < 0)
                .SumAsync(t => t.Amount);
            var availableBalance = fromAccount.Balance + pendingHolds;

            if (availableBalance < amount)
                throw new InvalidOperationException("Insufficient funds");

            var desc = description ?? $"Transfer to {toAccount.Name}";
            var now = dateTime.UtcNow;

            // Internal transfers settle immediately
            fromAccount.Balance -= amount;
            var fromTxn = new Transaction
            {
                AccountId = fromAccountId,
                Amount = -amount,
                Description = desc,
                TransactionType = TransactionType.Transfer,
                Status = TransactionStatus.Settled,
                SettledAt = now,
            };

            toAccount.Balance += amount;
            var toTxn = new Transaction
            {
                AccountId = toAccountId,
                Amount = amount,
                Description = $"Transfer from {fromAccount.Name}",
                TransactionType = TransactionType.Transfer,
                Status = TransactionStatus.Settled,
                SettledAt = now,
            };

            db.Transactions.AddRange(fromTxn, toTxn);

            try
            {
                await db.SaveChangesAsync();
                return (fromTxn, toTxn);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxRetries - 1)
            {
                db.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException("Unable to complete transfer due to concurrent access. Please try again.");
    }

    public async Task<object?> LookupAccountAsync(string bsb, string accountNumber)
    {
        var account = await db.Accounts
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Bsb == bsb && a.AccountNumber == accountNumber);

        if (account is null) return null;

        return new
        {
            account.Name,
            account.AccountType,
            AccountTypeName = account.AccountType.ToString(),
            CustomerName = account.Customer.FirstName + " " + account.Customer.LastName
        };
    }

    public async Task<(Transaction from, Transaction to)> PayAsync(
        int callerCustomerId, int fromAccountId, string toBsb, string toAccountNumber, decimal amount, string? description = null, string? reference = null)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Payment amount must be positive");

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            var fromAccount = await db.Accounts.FindAsync(fromAccountId)
                ?? throw new InvalidOperationException("Source account not found");

            if (fromAccount.CustomerId != callerCustomerId)
                throw new InvalidOperationException("You do not own this account");

            var toAccount = await db.Accounts
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Bsb == toBsb && a.AccountNumber == toAccountNumber)
                ?? throw new InvalidOperationException("Destination account not found. Check the BSB and account number.");

            if (fromAccount.CustomerId == toAccount.CustomerId)
                throw new InvalidOperationException("Use Transfer for payments between your own accounts");

            if (fromAccount.AccountType == AccountType.HomeLoan)
                throw new InvalidOperationException("Cannot pay from a home loan account");

            if (toAccount.AccountType == AccountType.HomeLoan)
                throw new InvalidOperationException("Cannot pay into a home loan account");

            var pendingHolds = await db.Transactions
                .Where(t => t.AccountId == fromAccountId && t.Status == TransactionStatus.Pending && t.Amount < 0)
                .SumAsync(t => t.Amount);
            var availableBalance = fromAccount.Balance + pendingHolds;

            if (availableBalance < amount)
                throw new InvalidOperationException("Insufficient funds");

            // Force RowVersion check on fromAccount for concurrency protection
            db.Entry(fromAccount).State = EntityState.Modified;

            var desc = description ?? $"Payment to {toAccount.Customer.FirstName} {toAccount.Customer.LastName}";
            var recipientRef = string.IsNullOrWhiteSpace(reference)
                ? $"Payment from {fromAccount.Name}"
                : reference;

            var now = dateTime.UtcNow;

            // Outgoing payment is pending (like a card payment)
            var fromTxn = new Transaction
            {
                AccountId = fromAccountId,
                Amount = -amount,
                Description = desc,
                TransactionType = TransactionType.Transfer,
                Status = TransactionStatus.Pending,
            };

            // Incoming credit settles immediately for the recipient
            toAccount.Balance += amount;
            var toTxn = new Transaction
            {
                AccountId = toAccount.Id,
                Amount = amount,
                Description = recipientRef,
                TransactionType = TransactionType.Transfer,
                Status = TransactionStatus.Settled,
                SettledAt = now,
            };

            db.Transactions.AddRange(fromTxn, toTxn);

            try
            {
                await db.SaveChangesAsync();
                return (fromTxn, toTxn);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxRetries - 1)
            {
                db.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException("Unable to complete payment due to concurrent access. Please try again.");
    }

    /// <summary>
    /// Settle a pending transaction: update ledger balance and mark as settled.
    /// </summary>
    public async Task<Transaction> SettleTransactionAsync(int transactionId)
    {
        var txn = await db.Transactions.Include(t => t.Account).FirstOrDefaultAsync(t => t.Id == transactionId)
            ?? throw new InvalidOperationException("Transaction not found");

        if (txn.Status != TransactionStatus.Pending)
            throw new InvalidOperationException($"Transaction is already {txn.Status}");

        // Apply the amount to the ledger balance
        txn.Account.Balance += txn.Amount;
        txn.Status = TransactionStatus.Settled;
        txn.SettledAt = dateTime.UtcNow;

        await db.SaveChangesAsync();
        return txn;
    }

    public async Task<List<Transaction>> GetTransactionsAsync(int accountId, int page = 1, int pageSize = 20)
    {
        return await db.Transactions
            .Where(t => t.AccountId == accountId && t.Status != TransactionStatus.Reversed)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
