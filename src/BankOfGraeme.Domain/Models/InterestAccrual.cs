namespace BankOfGraeme.Api.Models;

public class InterestAccrual
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public DateOnly AccrualDate { get; set; }
    public decimal DailyAmount { get; set; }
    public bool Posted { get; set; }
    public int? PostedTransactionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = null!;
    public Transaction? PostedTransaction { get; set; }
}
