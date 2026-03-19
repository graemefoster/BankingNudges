using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace BankOfGraeme.Tests;

public class TransferLinkageTests
{
    [Fact]
    public async Task TransferAsync_stamps_same_transfer_id_on_both_legs()
    {
        using var db = CreateDb();
        var customer = CreateCustomer("Ava", "Stone", "ava@example.com");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var fromAccount = CreateAccount(customer.Id, AccountType.Transaction, "10000001", "Everyday", 500m);
        var toAccount = CreateAccount(customer.Id, AccountType.Savings, "10000002", "Savings", 100m);
        db.Accounts.AddRange(fromAccount, toAccount);
        await db.SaveChangesAsync();

        var service = new AccountService(db, new FixedDateTimeProvider());

        var (from, to) = await service.TransferAsync(fromAccount.Id, toAccount.Id, 125m, "Move to savings");

        from.TransferId.ShouldNotBeNull();
        to.TransferId.ShouldBe(from.TransferId);
    }

    [Fact]
    public async Task PayAsync_stamps_same_transfer_id_on_both_legs()
    {
        using var db = CreateDb();
        var payer = CreateCustomer("Noah", "Payne", "noah@example.com");
        var payee = CreateCustomer("Ruby", "Cole", "ruby@example.com");
        db.Customers.AddRange(payer, payee);
        await db.SaveChangesAsync();

        var fromAccount = CreateAccount(payer.Id, AccountType.Transaction, "10000003", "Payer Everyday", 800m);
        var toAccount = CreateAccount(payee.Id, AccountType.Transaction, "10000004", "Payee Everyday", 50m);
        db.Accounts.AddRange(fromAccount, toAccount);
        await db.SaveChangesAsync();

        var service = new AccountService(db, new FixedDateTimeProvider());

        var (from, to) = await service.PayAsync(
            payer.Id, fromAccount.Id, toAccount.Bsb, toAccount.AccountNumber, 210m, "Rent share", "NOAH");

        from.TransferId.ShouldNotBeNull();
        to.TransferId.ShouldBe(from.TransferId);
        from.Status.ShouldBe(TransactionStatus.Settled);
        to.Status.ShouldBe(TransactionStatus.Settled);
    }

    [Fact]
    public async Task Scheduled_payment_with_internal_payee_stamps_same_transfer_id_on_both_legs()
    {
        using var db = CreateDb();
        var now = new DateTime(2025, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        var today = DateOnly.FromDateTime(now);
        var payer = CreateCustomer("Grace", "West", "grace@example.com");
        var payee = CreateCustomer("Liam", "Hart", "liam@example.com");
        db.Customers.AddRange(payer, payee);
        await db.SaveChangesAsync();

        var fromAccount = CreateAccount(payer.Id, AccountType.Transaction, "10000005", "Grace Everyday", 900m);
        var toAccount = CreateAccount(payee.Id, AccountType.Transaction, "10000006", "Liam Everyday", 150m);
        db.Accounts.AddRange(fromAccount, toAccount);
        await db.SaveChangesAsync();

        db.ScheduledPayments.Add(new ScheduledPayment
        {
            AccountId = fromAccount.Id,
            PayeeName = "Liam Hart",
            PayeeBsb = toAccount.Bsb,
            PayeeAccountNumber = toAccount.AccountNumber,
            PayeeAccountId = toAccount.Id,
            Amount = 75m,
            Description = "Shared bills",
            Reference = "GRACE",
            Frequency = ScheduleFrequency.Monthly,
            StartDate = today,
            NextDueDate = today,
            IsActive = true,
        });
        await db.SaveChangesAsync();

        var service = new ScheduledPaymentService(db, NullLogger<ScheduledPaymentService>.Instance);

        var executed = await service.ExecuteDuePaymentsAsync(today);

        executed.ShouldBe(1);
        var pair = await db.Transactions
            .Where(t => t.AccountId == fromAccount.Id || t.AccountId == toAccount.Id)
            .Where(t => t.TransactionType == TransactionType.DirectDebit)
            .OrderBy(t => t.Amount)
            .ToListAsync();

        pair.Count.ShouldBe(2);
        pair[0].TransferId.ShouldNotBeNull();
        pair[1].TransferId.ShouldBe(pair[0].TransferId);
    }

    [Fact]
    public async Task One_sided_transactions_do_not_get_transfer_id()
    {
        using var db = CreateDb();
        var customer = CreateCustomer("Mia", "Young", "mia@example.com");
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var account = CreateAccount(customer.Id, AccountType.Transaction, "10000007", "Everyday", 500m);
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        var service = new AccountService(db, new FixedDateTimeProvider());

        var deposit = await service.DepositAsync(account.Id, 80m, "Cash deposit");
        var withdrawal = await service.WithdrawAsync(account.Id, 20m, "Coffee");

        deposit.TransferId.ShouldBeNull();
        withdrawal.TransferId.ShouldBeNull();
    }

    [Fact]
    public void Seeded_fraud_transfers_share_transfer_ids()
    {
        using var db = CreateSeededDb();
        var marcus = FindMuleAccount(db, "Marcus", "Webb");

        var outgoing = db.Transactions
            .Where(t => t.AccountId == marcus.Id
                && t.Amount < 0
                && t.TransactionType == TransactionType.Transfer)
            .ToList();

        outgoing.ShouldNotBeEmpty();

        foreach (var debit in outgoing)
        {
            debit.TransferId.ShouldNotBeNull();

            var matchingCredit = db.Transactions.SingleOrDefault(t =>
                t.AccountId != marcus.Id &&
                t.TransferId == debit.TransferId &&
                t.Amount == -debit.Amount);

            matchingCredit.ShouldNotBeNull(
                $"Debit of {debit.Amount} should have an exact linked credit via TransferId");
        }
    }

    private static Customer CreateCustomer(string firstName, string lastName, string email) =>
        new()
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            DateOfBirth = new DateOnly(1990, 1, 1),
        };

    private static Account CreateAccount(int customerId, AccountType accountType, string accountNumber, string name, decimal balance) =>
        new()
        {
            CustomerId = customerId,
            AccountType = accountType,
            Bsb = "062-000",
            AccountNumber = accountNumber,
            Name = name,
            Balance = balance,
            IsActive = true,
        };

    private static Account FindMuleAccount(BankDbContext db, string first, string last)
    {
        var customer = db.Customers.Single(c => c.FirstName == first && c.LastName == last);
        return db.Accounts.Single(a => a.CustomerId == customer.Id && a.AccountType == AccountType.Transaction);
    }

    private static BankDbContext CreateSeededDb()
    {
        var now = new DateTime(2025, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        var dateTime = new FixedDateTimeProvider(now);
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new BankDbContext(options, dateTime);
        SeedData.Seed(db, dateTime);
        return db;
    }

    private static BankDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BankDbContext(options, new FixedDateTimeProvider());
    }

    private sealed class FixedDateTimeProvider(DateTime? now = null) : IDateTimeProvider
    {
        private readonly DateTime _now = now ?? new DateTime(2025, 9, 15, 12, 0, 0, DateTimeKind.Utc);

        public DateTime UtcNow => _now;
        public DateOnly Today => DateOnly.FromDateTime(_now);
        public int DaysAdvanced => 0;
        public Task AdvanceDaysAsync(int days) => Task.CompletedTask;
        public Task ResetAsync() => Task.CompletedTask;
    }
}
