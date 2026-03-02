namespace BankOfGraeme.Api.Models;

public class AccountBalanceSnapshot
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public DateOnly SnapshotDate { get; set; }
    public decimal LedgerBalance { get; set; }
    public decimal AvailableBalance { get; set; }
    public DateTime CreatedAt { get; set; }

    public Account Account { get; set; } = null!;
}
