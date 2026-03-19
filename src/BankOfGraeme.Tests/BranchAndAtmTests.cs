using BankOfGraeme.Api.Data;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace BankOfGraeme.Tests;

public class BranchAndAtmTests
{
    private static BankDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BankDbContext(options);
    }

    [Fact]
    public void Branch_can_be_persisted_and_retrieved()
    {
        using var db = CreateDb();

        db.Branches.Add(new Branch
        {
            Name = "Test Branch",
            Address = "1 Main St",
            Suburb = "Sydney",
            State = "NSW",
            Postcode = "2000",
            Latitude = -33.8688m,
            Longitude = 151.2093m,
        });
        db.SaveChanges();

        var branch = db.Branches.Single();
        branch.Name.ShouldBe("Test Branch");
        branch.Latitude.ShouldBe(-33.8688m);
        branch.Longitude.ShouldBe(151.2093m);
    }

    [Fact]
    public void Atm_can_be_linked_to_branch()
    {
        using var db = CreateDb();

        var branch = new Branch
        {
            Name = "Branch One",
            Address = "1 Main St",
            Suburb = "Sydney",
            State = "NSW",
            Postcode = "2000",
            Latitude = -33.8688m,
            Longitude = 151.2093m,
        };
        db.Branches.Add(branch);
        db.SaveChanges();

        var atm = new Atm
        {
            LocationName = "Branch One ATM",
            Address = "1 Main St",
            Suburb = "Sydney",
            State = "NSW",
            Postcode = "2000",
            Latitude = -33.8688m,
            Longitude = 151.2093m,
            BranchId = branch.Id,
        };
        db.Atms.Add(atm);
        db.SaveChanges();

        var loaded = db.Atms.Include(a => a.Branch).Single();
        loaded.BranchId.ShouldBe(branch.Id);
        loaded.Branch.ShouldNotBeNull();
        loaded.Branch!.Name.ShouldBe("Branch One");
    }

    [Fact]
    public void Atm_can_exist_without_branch()
    {
        using var db = CreateDb();

        db.Atms.Add(new Atm
        {
            LocationName = "Standalone ATM",
            Address = "Shopping Centre",
            Suburb = "Parramatta",
            State = "NSW",
            Postcode = "2150",
            Latitude = -33.8148m,
            Longitude = 151.0034m,
            BranchId = null,
        });
        db.SaveChanges();

        var atm = db.Atms.Single();
        atm.BranchId.ShouldBeNull();
    }

    [Fact]
    public void Branch_has_navigation_to_its_atms()
    {
        using var db = CreateDb();

        var branch = new Branch
        {
            Name = "Branch Two",
            Address = "10 King St",
            Suburb = "Melbourne",
            State = "VIC",
            Postcode = "3000",
            Latitude = -37.8136m,
            Longitude = 144.9631m,
        };
        db.Branches.Add(branch);
        db.SaveChanges();

        db.Atms.AddRange(
            new Atm { LocationName = "ATM A", Address = "10 King St", Suburb = "Melbourne", State = "VIC", Postcode = "3000", Latitude = -37.8136m, Longitude = 144.9631m, BranchId = branch.Id },
            new Atm { LocationName = "ATM B", Address = "11 King St", Suburb = "Melbourne", State = "VIC", Postcode = "3000", Latitude = -37.8140m, Longitude = 144.9635m, BranchId = branch.Id }
        );
        db.SaveChanges();

        var loaded = db.Branches.Include(b => b.Atms).Single();
        loaded.Atms.Count.ShouldBe(2);
    }

    [Fact]
    public void Transaction_can_reference_atm_for_withdrawal()
    {
        using var db = CreateDb();

        var customer = new Customer { FirstName = "Test", LastName = "User", Email = "test@test.com" };
        db.Customers.Add(customer);
        db.SaveChanges();

        var account = new Account
        {
            CustomerId = customer.Id,
            AccountType = AccountType.Transaction,
            Bsb = "062-000",
            AccountNumber = "99999001",
            Name = "Everyday",
            Balance = 1000m,
            IsActive = true,
        };
        db.Accounts.Add(account);
        db.SaveChanges();

        var atm = new Atm
        {
            LocationName = "Test ATM",
            Address = "1 Test St",
            Suburb = "Sydney",
            State = "NSW",
            Postcode = "2000",
            Latitude = -33.87m,
            Longitude = 151.21m,
        };
        db.Atms.Add(atm);
        db.SaveChanges();

        var txn = new Transaction
        {
            AccountId = account.Id,
            Amount = -60m,
            Description = "ATM WITHDRAWAL - Test ATM",
            TransactionType = TransactionType.Withdrawal,
            Status = TransactionStatus.Settled,
            AtmId = atm.Id,
            CreatedAt = DateTime.UtcNow,
            SettledAt = DateTime.UtcNow,
        };
        db.Transactions.Add(txn);
        db.SaveChanges();

        var loaded = db.Transactions.Include(t => t.Atm).Single();
        loaded.AtmId.ShouldBe(atm.Id);
        loaded.Atm.ShouldNotBeNull();
        loaded.Atm!.LocationName.ShouldBe("Test ATM");
        loaded.BranchId.ShouldBeNull();
    }

    [Fact]
    public void Transaction_can_reference_branch_for_deposit()
    {
        using var db = CreateDb();

        var customer = new Customer { FirstName = "Test", LastName = "User", Email = "test@test.com" };
        db.Customers.Add(customer);
        db.SaveChanges();

        var account = new Account
        {
            CustomerId = customer.Id,
            AccountType = AccountType.Transaction,
            Bsb = "062-000",
            AccountNumber = "99999002",
            Name = "Everyday",
            Balance = 500m,
            IsActive = true,
        };
        db.Accounts.Add(account);
        db.SaveChanges();

        var branch = new Branch
        {
            Name = "Test Branch",
            Address = "5 Queen St",
            Suburb = "Brisbane",
            State = "QLD",
            Postcode = "4000",
            Latitude = -27.47m,
            Longitude = 153.03m,
        };
        db.Branches.Add(branch);
        db.SaveChanges();

        var txn = new Transaction
        {
            AccountId = account.Id,
            Amount = 200m,
            Description = "BRANCH DEPOSIT - Test Branch",
            TransactionType = TransactionType.Deposit,
            Status = TransactionStatus.Settled,
            BranchId = branch.Id,
            CreatedAt = DateTime.UtcNow,
            SettledAt = DateTime.UtcNow,
        };
        db.Transactions.Add(txn);
        db.SaveChanges();

        var loaded = db.Transactions.Include(t => t.Branch).Single();
        loaded.BranchId.ShouldBe(branch.Id);
        loaded.Branch.ShouldNotBeNull();
        loaded.Branch!.Name.ShouldBe("Test Branch");
        loaded.AtmId.ShouldBeNull();
    }

    [Fact]
    public void Existing_transactions_unaffected_by_new_nullable_fks()
    {
        using var db = CreateDb();

        var customer = new Customer { FirstName = "Test", LastName = "User", Email = "test@test.com" };
        db.Customers.Add(customer);
        db.SaveChanges();

        var account = new Account
        {
            CustomerId = customer.Id,
            AccountType = AccountType.Transaction,
            Bsb = "062-000",
            AccountNumber = "99999003",
            Name = "Everyday",
            Balance = 1000m,
            IsActive = true,
        };
        db.Accounts.Add(account);
        db.SaveChanges();

        // A regular withdrawal with no ATM or branch
        var txn = new Transaction
        {
            AccountId = account.Id,
            Amount = -25m,
            Description = "WOOLWORTHS 234",
            TransactionType = TransactionType.Withdrawal,
            Status = TransactionStatus.Settled,
            CreatedAt = DateTime.UtcNow,
            SettledAt = DateTime.UtcNow,
        };
        db.Transactions.Add(txn);
        db.SaveChanges();

        var loaded = db.Transactions.Single();
        loaded.AtmId.ShouldBeNull();
        loaded.BranchId.ShouldBeNull();
        loaded.Atm.ShouldBeNull();
        loaded.Branch.ShouldBeNull();
    }
}
