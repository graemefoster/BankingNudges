namespace BankOfGraeme.Api.Models;

public enum NudgeUrgency
{
    LOW,
    MEDIUM,
    HIGH
}

public enum NudgeCategory
{
    CASHFLOW,
    SAVINGS,
    SPENDING,
    UPCOMING_PAYMENT,
    TRAVEL
}

public enum NudgeStatus
{
    PENDING,
    SENT,
    ACCEPTED,
    DISMISSED,
    SNOOZED,
    EXPIRED
}

public class Nudge
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public required string Message { get; set; }
    public required string Cta { get; set; }
    public NudgeUrgency Urgency { get; set; }
    public NudgeCategory Category { get; set; }
    public required string Reasoning { get; set; }
    public NudgeStatus Status { get; set; } = NudgeStatus.PENDING;
    public required string ContextSnapshot { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    public Customer Customer { get; set; } = null!;
}
