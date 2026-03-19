namespace BankOfGraeme.Api.Models;

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer,
    Interest,
    Repayment,
    Adjustment,
    DirectDebit
}

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public required string Description { get; set; }
    public TransactionType TransactionType { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Settled;
    public string? FailureReason { get; set; }
    public DateTime? SettledAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Foreign currency fields — null for domestic (AUD) transactions
    public string? OriginalCurrency { get; set; }
    public decimal? OriginalAmount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal? FeeAmount { get; set; }

    // Location fields — set for ATM withdrawals / branch deposits
    public int? BranchId { get; set; }
    public int? AtmId { get; set; }

    public Account Account { get; set; } = null!;
    public Branch? Branch { get; set; }
    public Atm? Atm { get; set; }
}
