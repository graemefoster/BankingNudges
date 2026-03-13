namespace BankOfGraeme.Api.Models;

public class CustomerHoliday
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public required string Destination { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
}
