using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NSubstitute;
using Shouldly;

namespace BankOfGraeme.Tests;

/// <summary>
/// Validates that the APP fraud seed scenario produces the expected mule
/// network and transaction patterns that Neo4j Cypher queries rely on.
/// </summary>
public class FraudScenarioTests
{
    private static readonly InMemoryDatabaseRoot DatabaseRoot = new();
    private static readonly object SeedLock = new();
    private static readonly DateTime SeedNow = new(2025, 9, 15, 12, 0, 0, DateTimeKind.Utc);
    private const string DatabaseName = "FraudScenarioTests";
    private static bool isSeeded;

    private static readonly (string First, string Last)[] MuleNames =
    [
        ("Marcus", "Webb"), ("Jade", "Thornton"), ("Ryan", "Kovac"), ("Priya", "Desai")
    ];

    private static BankDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(DatabaseName, DatabaseRoot)
            .Options;
        return new BankDbContext(options);
    }

    private static void EnsureSeeded()
    {
        if (isSeeded) return;

        lock (SeedLock)
        {
            if (isSeeded) return;

            using var db = CreateDb();
            var dateTime = Substitute.For<IDateTimeProvider>();
            dateTime.UtcNow.Returns(SeedNow);
            dateTime.Today.Returns(DateOnly.FromDateTime(SeedNow));
            SeedData.Seed(db, dateTime);
            isSeeded = true;
        }
    }

    private static Account FindMuleAccount(BankDbContext db, string first, string last)
    {
        var customer = db.Customers.First(c => c.FirstName == first && c.LastName == last);
        return db.Accounts.First(a =>
            a.CustomerId == customer.Id && a.AccountType == AccountType.Transaction);
    }

    private static BankDbContext CreateSeededDb()
    {
        EnsureSeeded();
        return CreateDb();
    }

    [Fact]
    public void Fraud_scenario_creates_four_mule_customers()
    {
        using var db = CreateSeededDb();

        var muleNames = new[] { "Marcus Webb", "Jade Thornton", "Ryan Kovac", "Priya Desai" };
        foreach (var name in muleNames)
        {
            var parts = name.Split(' ');
            db.Customers
                .Any(c => c.FirstName == parts[0] && c.LastName == parts[1])
                .ShouldBeTrue($"Mule customer '{name}' should exist");
        }
    }

    [Fact]
    public void Each_mule_has_a_transaction_account()
    {
        using var db = CreateSeededDb();

        foreach (var (first, last) in MuleNames)
        {
            var customer = db.Customers.First(c => c.FirstName == first && c.LastName == last);
            db.Accounts
                .Any(a => a.CustomerId == customer.Id && a.AccountType == AccountType.Transaction)
                .ShouldBeTrue($"Mule '{first} {last}' should have a Transaction account");
        }
    }

    [Fact]
    public void Collector_receives_payments_from_five_distinct_accounts()
    {
        using var db = CreateSeededDb();

        var collectorAccount = FindMuleAccount(db, "Marcus", "Webb");

        var incomingCredits = db.Transactions
            .Where(t => t.AccountId == collectorAccount.Id
                && t.Amount > 0
                && t.TransactionType == TransactionType.Transfer)
            .AsEnumerable()
            .Where(t => t.Description.StartsWith("PAYMENT FROM"))
            .ToList();

        incomingCredits.Count.ShouldBe(5, "Collector should receive 5 victim payments");
    }

    [Fact]
    public void Money_cascades_through_all_four_mule_accounts()
    {
        using var db = CreateSeededDb();

        var muleAccounts = MuleNames
            .Select(m => FindMuleAccount(db, m.First, m.Last)).ToList();

        // Marcus (collector): should have outgoing transfers
        db.Transactions.Count(t =>
            t.AccountId == muleAccounts[0].Id && t.Amount < 0
            && t.TransactionType == TransactionType.Transfer).ShouldBeGreaterThan(0);

        // Jade (layer1): should have both incoming and outgoing transfers
        db.Transactions.Count(t =>
            t.AccountId == muleAccounts[1].Id && t.Amount > 0
            && t.TransactionType == TransactionType.Transfer).ShouldBeGreaterThan(0);
        db.Transactions.Count(t =>
            t.AccountId == muleAccounts[1].Id && t.Amount < 0
            && t.TransactionType == TransactionType.Transfer).ShouldBeGreaterThan(0);

        // Ryan (layer2): should have both incoming and outgoing transfers
        db.Transactions.Count(t =>
            t.AccountId == muleAccounts[2].Id && t.Amount > 0
            && t.TransactionType == TransactionType.Transfer).ShouldBeGreaterThan(0);
        db.Transactions.Count(t =>
            t.AccountId == muleAccounts[2].Id && t.Amount < 0
            && t.TransactionType == TransactionType.Transfer).ShouldBeGreaterThan(0);

        // Priya (cash-out): should have incoming transfers and ATM withdrawals
        db.Transactions.Count(t =>
            t.AccountId == muleAccounts[3].Id && t.Amount > 0
            && t.TransactionType == TransactionType.Transfer).ShouldBeGreaterThan(0);
        db.Transactions.Count(t =>
            t.AccountId == muleAccounts[3].Id && t.Amount < 0
            && t.TransactionType == TransactionType.Withdrawal).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Commission_reduces_total_amount_at_each_hop()
    {
        using var db = CreateSeededDb();

        var muleAccounts = MuleNames
            .Select(m => FindMuleAccount(db, m.First, m.Last)).ToList();

        decimal IncomingTransfers(int accountId) =>
            db.Transactions
                .Where(t => t.AccountId == accountId && t.Amount > 0
                    && t.TransactionType == TransactionType.Transfer)
                .Sum(t => t.Amount);

        var collectorIn = IncomingTransfers(muleAccounts[0].Id);
        var layer1In = IncomingTransfers(muleAccounts[1].Id);
        var layer2In = IncomingTransfers(muleAccounts[2].Id);
        var cashOutIn = IncomingTransfers(muleAccounts[3].Id);

        collectorIn.ShouldBeGreaterThan(0m, "Collector should have incoming payments");
        layer1In.ShouldBeGreaterThan(0m, "Layer1 should have incoming transfers");
        layer2In.ShouldBeGreaterThan(0m, "Layer2 should have incoming transfers");
        cashOutIn.ShouldBeGreaterThan(0m, "CashOut should have incoming transfers");

        collectorIn.ShouldBeGreaterThan(layer1In,
            "Collector total should be > Layer1 total (commission taken)");
        layer1In.ShouldBeGreaterThan(layer2In,
            "Layer1 total should be > Layer2 total (commission taken)");
        layer2In.ShouldBeGreaterThan(cashOutIn,
            "Layer2 total should be > CashOut total (commission taken)");
    }

    [Fact]
    public void Established_mules_have_camouflage_transactions()
    {
        using var db = CreateSeededDb();

        // Jade (established mule) should have salary deposits and grocery spending
        var jadeAccount = FindMuleAccount(db, "Jade", "Thornton");

        db.Transactions
            .Any(t => t.AccountId == jadeAccount.Id && t.Description == "SALARY PAYMENT")
            .ShouldBeTrue("Established mule Jade should have salary deposits");

        // Marcus (burner mule) should have very few non-fraud transactions
        var marcusAccount = FindMuleAccount(db, "Marcus", "Webb");

        var marcusNonTransferTxns = db.Transactions
            .Count(t => t.AccountId == marcusAccount.Id
                && t.TransactionType != TransactionType.Transfer);

        marcusNonTransferTxns.ShouldBeLessThan(5,
            "Burner mule Marcus should have very few non-fraud transactions");
    }

    [Fact]
    public void Transfer_pairs_share_same_created_at_for_graph_matching()
    {
        using var db = CreateSeededDb();

        var marcusAccount = FindMuleAccount(db, "Marcus", "Webb");

        var outgoing = db.Transactions
            .Where(t => t.AccountId == marcusAccount.Id
                && t.Amount < 0
                && t.TransactionType == TransactionType.Transfer)
            .ToList();

        foreach (var debit in outgoing)
        {
            var matchingCredit = db.Transactions
                .FirstOrDefault(t => t.AccountId != marcusAccount.Id
                    && t.CreatedAt == debit.CreatedAt
                    && t.Amount == -debit.Amount
                    && t.TransactionType == TransactionType.Transfer);

            matchingCredit.ShouldNotBeNull(
                $"Debit of {debit.Amount} at {debit.CreatedAt} should have a matching credit on another account");
        }
    }
}
