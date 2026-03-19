using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Services;
using BankOfGraeme.Neo4j;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

var services = new ServiceCollection();

services.AddDbContext<BankDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

services.AddScoped<IDateTimeProvider, DatabaseDateTimeProvider>();

var neo4jUri = configuration["Neo4j:Uri"] ?? "bolt://localhost:7687";
var neo4jUser = configuration["Neo4j:Username"] ?? "neo4j";
var neo4jPassword = configuration["Neo4j:Password"] ?? "neo4jdev123";

services.AddSingleton(_ => GraphDatabase.Driver(neo4jUri, AuthTokens.Basic(neo4jUser, neo4jPassword)));
services.AddScoped<Neo4jGraphSyncService>();

await using var serviceProvider = services.BuildServiceProvider();

Console.WriteLine("🏦 Bank of Graeme → Neo4j Graph Sync");
Console.WriteLine($"  PostgreSQL: {configuration.GetConnectionString("DefaultConnection")?.Split(';').FirstOrDefault()}");
Console.WriteLine($"  Neo4j:      {neo4jUri}");
Console.WriteLine();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var stopwatch = System.Diagnostics.Stopwatch.StartNew();

await using (var scope = serviceProvider.CreateAsyncScope())
{
    var driver = scope.ServiceProvider.GetRequiredService<IDriver>();
    try
    {
        await driver.VerifyConnectivityAsync();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ Cannot connect to Neo4j at {neo4jUri}: {ex.Message}");
        Console.Error.WriteLine("   Is the Neo4j container running? Try: docker compose up -d neo4j");
        return 1;
    }

    var syncService = scope.ServiceProvider.GetRequiredService<Neo4jGraphSyncService>();
    await syncService.SyncAsync(cts.Token);
}

stopwatch.Stop();
Console.WriteLine();
Console.WriteLine($"⏱️  Completed in {stopwatch.Elapsed.TotalSeconds:F1}s");
Console.WriteLine($"🌐 Explore your graph: http://localhost:7474");

return 0;
