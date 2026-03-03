namespace BankOfGraeme.Api.Models;

public enum ScheduleFrequency
{
    OneOff,
    Weekly,
    Fortnightly,
    Monthly,
    Quarterly,
    Yearly
}

public class ScheduledPayment
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public required string PayeeName { get; set; }
    public string? PayeeBsb { get; set; }
    public string? PayeeAccountNumber { get; set; }
    public int? PayeeAccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public ScheduleFrequency Frequency { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly NextDueDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Account Account { get; set; } = null!;
    public Account? PayeeAccount { get; set; }
}
