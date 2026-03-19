namespace BankOfGraeme.Api.Models;

public class Branch
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public required string Suburb { get; set; }
    public required string State { get; set; }
    public required string Postcode { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<Atm> Atms { get; set; } = [];
}
