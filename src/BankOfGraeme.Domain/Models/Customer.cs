namespace BankOfGraeme.Api.Models;

public class Customer
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<Account> Accounts { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}";
}
