namespace BankOfGraeme.Api.Models;

public class SystemSettings
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
}
