using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace BankOfGraeme.Tests;

public class TravelSignalDetectionTests : IDisposable
{
    private readonly BankDbContext _db;
    private readonly NudgeContextAssembler _assembler;
    private readonly DateTime _now = new(2026, 3, 12, 12, 0, 0);

    public TravelSignalDetectionTests()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dateTime = Substitute.For<IDateTimeProvider>();
        dateTime.UtcNow.Returns(_now);
        dateTime.Today.Returns(DateOnly.FromDateTime(_now));

        _db = new BankDbContext(options, dateTime);

        _assembler = new NudgeContextAssembler(
            _db, dateTime,
            new NudgePatternDetector(),
            new NudgeSignalDetector(),
            Substitute.For<ILogger<NudgeContextAssembler>>());

        SeedBaseData();
    }

    private void SeedBaseData()
    {
        _db.Customers.Add(new Customer
        {
            Id = 1,
            FirstName = "Test",
            LastName = "Customer",
            Email = "test@example.com",
            DateOfBirth = new DateOnly(1990, 1, 1)
        });

        _db.Accounts.Add(new Account
        {
            Id = 1,
            CustomerId = 1,
            AccountType = AccountType.Transaction,
            Bsb = "062-000",
            AccountNumber = "10000001",
            Name = "Everyday",
            Balance = 5000m
        });

        _db.SaveChanges();
    }

    [Fact]
    public async Task ForeignSpend_NoHoliday_FiresSignal()
    {
        // Customer has foreign spend in the last 7 days with no registered holiday
        AddForeignTransaction(daysAgo: 2, currency: "JPY", originalAmount: 5000m, exchangeRate: 97m, amount: -54.64m);

        var ctx = await _assembler.AssembleAsync(1);

        ctx.ShouldNotBeNull();
        ctx.Signals.ShouldContain(s => s.Type == SignalType.FOREIGN_SPEND_NO_HOLIDAY);
        var signal = ctx.Signals.First(s => s.Type == SignalType.FOREIGN_SPEND_NO_HOLIDAY);
        signal.Category.ShouldBe("JPY");
    }

    [Fact]
    public async Task ForeignSpend_WithCoveringHoliday_NoSignal()
    {
        // Customer has foreign spend AND a registered holiday covering the dates
        AddForeignTransaction(daysAgo: 2, currency: "JPY", originalAmount: 5000m, exchangeRate: 97m, amount: -54.64m);

        _db.CustomerHolidays.Add(new CustomerHoliday
        {
            CustomerId = 1,
            Destination = "Japan",
            StartDate = DateOnly.FromDateTime(_now.AddDays(-5)),
            EndDate = DateOnly.FromDateTime(_now.AddDays(3)),
        });
        _db.SaveChanges();

        var ctx = await _assembler.AssembleAsync(1);

        ctx.ShouldNotBeNull();
        ctx.Signals.ShouldNotContain(s => s.Type == SignalType.FOREIGN_SPEND_NO_HOLIDAY);
    }

    [Fact]
    public async Task ForeignSpend_MultipleCurrencies_ShowsAll()
    {
        AddForeignTransaction(daysAgo: 2, currency: "JPY", originalAmount: 5000m, exchangeRate: 97m, amount: -54.64m);
        AddForeignTransaction(daysAgo: 1, currency: "THB", originalAmount: 800m, exchangeRate: 23.5m, amount: -37.02m);

        var ctx = await _assembler.AssembleAsync(1);

        ctx.ShouldNotBeNull();
        var signal = ctx.Signals.First(s => s.Type == SignalType.FOREIGN_SPEND_NO_HOLIDAY);
        signal.Category.ShouldNotBeNull();
        signal.Category.ShouldContain("JPY");
        signal.Category.ShouldContain("THB");
    }

    [Fact]
    public async Task ForeignSpend_OlderThan7Days_NoSignal()
    {
        // Foreign transaction from 10 days ago — outside the 7-day window
        AddForeignTransaction(daysAgo: 10, currency: "JPY", originalAmount: 5000m, exchangeRate: 97m, amount: -54.64m);

        var ctx = await _assembler.AssembleAsync(1);

        ctx.ShouldNotBeNull();
        ctx.Signals.ShouldNotContain(s => s.Type == SignalType.FOREIGN_SPEND_NO_HOLIDAY);
    }

    [Fact]
    public async Task FlightBooking_Over300_NoFutureHoliday_FiresSignal()
    {
        AddDomesticTransaction(daysAgo: 2, amount: -1850m, description: "QANTAS - EUROPE");

        var ctx = await _assembler.AssembleAsync(1);

        ctx.ShouldNotBeNull();
        ctx.Signals.ShouldContain(s => s.Type == SignalType.FLIGHT_BOOKING_DETECTED);
        var signal = ctx.Signals.First(s => s.Type == SignalType.FLIGHT_BOOKING_DETECTED);
        signal.PaymentMerchant.ShouldBe("QANTAS - EUROPE");
        signal.PaymentAmount.ShouldBe(1850m);
    }

    [Fact]
    public async Task FlightBooking_Under300_NoSignal()
    {
        // $250 at a flight vendor — below the $300 threshold
        AddDomesticTransaction(daysAgo: 2, amount: -250m, description: "WEBJET - DOMESTIC");

        var ctx = await _assembler.AssembleAsync(1);

        ctx.ShouldNotBeNull();
        ctx.Signals.ShouldNotContain(s => s.Type == SignalType.FLIGHT_BOOKING_DETECTED);
    }

    [Fact]
    public async Task FlightBooking_WithFutureHoliday_NoSignal()
    {
        AddDomesticTransaction(daysAgo: 2, amount: -1850m, description: "QANTAS - EUROPE");

        // Customer already registered a future holiday
        _db.CustomerHolidays.Add(new CustomerHoliday
        {
            CustomerId = 1,
            Destination = "Europe",
            StartDate = DateOnly.FromDateTime(_now.AddDays(14)),
            EndDate = DateOnly.FromDateTime(_now.AddDays(28)),
        });
        _db.SaveChanges();

        var ctx = await _assembler.AssembleAsync(1);

        ctx.ShouldNotBeNull();
        ctx.Signals.ShouldNotContain(s => s.Type == SignalType.FLIGHT_BOOKING_DETECTED);
    }

    [Fact]
    public async Task FlightBooking_NonFlightVendor_NoSignal()
    {
        // Large purchase at a non-flight merchant
        AddDomesticTransaction(daysAgo: 2, amount: -500m, description: "HARVEY NORMAN - SYDNEY");

        var ctx = await _assembler.AssembleAsync(1);

        ctx.ShouldNotBeNull();
        ctx.Signals.ShouldNotContain(s => s.Type == SignalType.FLIGHT_BOOKING_DETECTED);
    }

    [Fact]
    public async Task FlightBooking_PicksLargestAmount()
    {
        // Two flight bookings — should pick the larger one
        AddDomesticTransaction(daysAgo: 3, amount: -800m, description: "FLIGHT CENTRE - BALI");
        AddDomesticTransaction(daysAgo: 1, amount: -3200m, description: "QANTAS - EUROPE");

        var ctx = await _assembler.AssembleAsync(1);

        ctx.ShouldNotBeNull();
        var signal = ctx.Signals.First(s => s.Type == SignalType.FLIGHT_BOOKING_DETECTED);
        signal.PaymentAmount.ShouldBe(3200m);
        signal.PaymentMerchant.ShouldBe("QANTAS - EUROPE");
    }

    [Fact]
    public async Task NoForeignSpend_NoFlightBooking_NoTravelSignals()
    {
        // Only domestic transactions, no flight vendors
        AddDomesticTransaction(daysAgo: 1, amount: -45m, description: "WOOLWORTHS - SYDNEY");
        AddDomesticTransaction(daysAgo: 2, amount: -12m, description: "COLES - MELBOURNE");

        var ctx = await _assembler.AssembleAsync(1);

        ctx.ShouldNotBeNull();
        ctx.Signals.ShouldNotContain(s => s.Type == SignalType.FOREIGN_SPEND_NO_HOLIDAY);
        ctx.Signals.ShouldNotContain(s => s.Type == SignalType.FLIGHT_BOOKING_DETECTED);
    }

    private void AddForeignTransaction(int daysAgo, string currency, decimal originalAmount, decimal exchangeRate, decimal amount)
    {
        _db.Transactions.Add(new Transaction
        {
            AccountId = 1,
            Amount = amount,
            Description = $"FOREIGN MERCHANT - {currency}",
            TransactionType = TransactionType.Withdrawal,
            Status = TransactionStatus.Settled,
            OriginalCurrency = currency,
            OriginalAmount = originalAmount,
            ExchangeRate = exchangeRate,
            FeeAmount = Math.Abs(amount) * 0.03m,
            CreatedAt = _now.AddDays(-daysAgo),
            SettledAt = _now.AddDays(-daysAgo),
        });
        _db.SaveChanges();
    }

    private void AddDomesticTransaction(int daysAgo, decimal amount, string description)
    {
        _db.Transactions.Add(new Transaction
        {
            AccountId = 1,
            Amount = amount,
            Description = description,
            TransactionType = TransactionType.Withdrawal,
            Status = TransactionStatus.Settled,
            CreatedAt = _now.AddDays(-daysAgo),
            SettledAt = _now.AddDays(-daysAgo),
        });
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
