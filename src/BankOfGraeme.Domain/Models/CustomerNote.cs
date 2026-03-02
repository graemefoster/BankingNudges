namespace BankOfGraeme.Api.Models;

public class CustomerNote
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int StaffUserId { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
    public StaffUser StaffUser { get; set; } = null!;
}
