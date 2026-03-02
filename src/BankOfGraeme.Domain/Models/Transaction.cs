namespace BankOfGraeme.Api.Models;

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer,
    Interest,
    Repayment,
    Adjustment
}

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public required string Description { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = null!;
}
