namespace BankOfGraeme.Api.Models;

public enum AccountType
{
    Transaction,
    Savings,
    HomeLoan,
    Offset
}

public class Account
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public AccountType AccountType { get; set; }
    public required string Bsb { get; set; }
    public required string AccountNumber { get; set; }
    public required string Name { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Optimistic concurrency
    public uint RowVersion { get; set; }

    // Home loan specific
    public decimal? LoanAmount { get; set; }
    public decimal? InterestRate { get; set; }
    public int? LoanTermMonths { get; set; }

    // Offset links to a home loan
    public int? HomeLoanAccountId { get; set; }

    public Customer Customer { get; set; } = null!;
    public Account? HomeLoanAccount { get; set; }
    public List<Account> OffsetAccounts { get; set; } = [];
    public List<Transaction> Transactions { get; set; } = [];

    public string FormattedAccountNumber => $"{Bsb} {AccountNumber}";
}
