using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BankOfGraeme.Api.Data;

public class BankDbContextFactory : IDesignTimeDbContextFactory<BankDbContext>
{
    public BankDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BankDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=bankofgraeme;Username=bankadmin;Password=BankDev123!");
        return new BankDbContext(optionsBuilder.Options);
    }
}
