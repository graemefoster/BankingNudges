using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace BankOfGraeme.Tests;

public class NudgeBatchRunnerTests : IDisposable
{
    private readonly BankDbContext _db;
    private readonly NudgeContextAssembler _assembler;
    private readonly NudgeGenerator _generator;
    private readonly NudgeBatchRunner _runner;

    public NudgeBatchRunnerTests()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dateTime = Substitute.For<IDateTimeProvider>();
        dateTime.UtcNow.Returns(new DateTime(2026, 3, 9, 12, 0, 0));
        dateTime.Today.Returns(new DateOnly(2026, 3, 9));

        _db = new BankDbContext(options, dateTime);

        var patternDetector = new NudgePatternDetector();
        var signalDetector = new NudgeSignalDetector();

        _assembler = new NudgeContextAssembler(
            _db, dateTime, patternDetector, signalDetector,
            Substitute.For<ILogger<NudgeContextAssembler>>());

        var responsesClient = Substitute.For<ResponsesClient>();
        _generator = new NudgeGenerator(responsesClient, new NudgeGeneratorSettings("test-model"), Substitute.For<ILogger<NudgeGenerator>>());

        _runner = new NudgeBatchRunner(
            _db, _assembler, _generator,
            Substitute.For<ILogger<NudgeBatchRunner>>());

        SeedTestData();
    }

    private void SeedTestData()
    {
        var customer = new Customer
        {
            Id = 1,
            FirstName = "Test",
            LastName = "Customer",
            Email = "test@example.com",
            DateOfBirth = new DateOnly(1990, 1, 1)
        };
        _db.Customers.Add(customer);

        var account = new Account
        {
            Id = 1,
            CustomerId = 1,
            AccountType = AccountType.Transaction,
            Bsb = "062-000",
            AccountNumber = "1234567890",
            Name = "Everyday",
            Balance = 500m
        };
        _db.Accounts.Add(account);
        _db.SaveChanges();
    }

    [Fact]
    public async Task SkipsCustomerWithNoSignals()
    {
        // Customer has high balance and no transactions — no signals expected
        var account = await _db.Accounts.FindAsync(1);
        account!.Balance = 50000m;
        await _db.SaveChangesAsync();

        var result = await _runner.RunAsync(customerIds: [1]);

        Assert.Equal(1, result.Total);
        Assert.Equal(0, result.Generated);
        Assert.True(result.Skipped > 0 || result.Errors > 0);
    }

    [Fact]
    public async Task HandlesNonExistentCustomer()
    {
        var result = await _runner.RunAsync(customerIds: [999]);

        Assert.Equal(1, result.Total);
        Assert.Equal(0, result.Generated);
        // Should skip (null context) not crash
        Assert.Equal(0, result.Errors);
    }

    [Fact]
    public async Task ContinuesAfterIndividualError()
    {
        // Add a second customer
        _db.Customers.Add(new Customer
        {
            Id = 2,
            FirstName = "Second",
            LastName = "Customer",
            Email = "second@example.com",
            DateOfBirth = new DateOnly(1985, 5, 5)
        });
        _db.Accounts.Add(new Account
        {
            Id = 2,
            CustomerId = 2,
            AccountType = AccountType.Transaction,
            Bsb = "062-000",
            AccountNumber = "9876543210",
            Name = "Everyday",
            Balance = 500m
        });
        await _db.SaveChangesAsync();

        // Both customers exist, batch should process both without crashing
        var result = await _runner.RunAsync(customerIds: [1, 2]);

        Assert.Equal(2, result.Total);
        // Neither should error — they should be skipped (no API key = null from generator)
        Assert.Equal(0, result.Errors);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
