using Microsoft.EntityFrameworkCore;
using BankOfGraeme.Api.Models;
using BankOfGraeme.Api.Services;

namespace BankOfGraeme.Api.Data;

public class BankDbContext : DbContext
{
    private readonly IDateTimeProvider? _dateTimeProvider;

    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options) { }

    public BankDbContext(DbContextOptions<BankDbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<StaffUser> StaffUsers => Set<StaffUser>();
    public DbSet<CustomerNote> CustomerNotes => Set<CustomerNote>();
    public DbSet<InterestAccrual> InterestAccruals => Set<InterestAccrual>();
    public DbSet<AccountBalanceSnapshot> AccountBalanceSnapshots => Set<AccountBalanceSnapshot>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();
    public DbSet<ScheduledPayment> ScheduledPayments => Set<ScheduledPayment>();
    public DbSet<Nudge> Nudges => Set<Nudge>();
    public DbSet<CustomerHoliday> CustomerHolidays => Set<CustomerHoliday>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Atm> Atms => Set<Atm>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampCreatedAt();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampCreatedAt();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampCreatedAt()
    {
        var now = _dateTimeProvider?.UtcNow ?? DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added) continue;
            var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
            if (prop is not null && prop.CurrentValue is DateTime dt && dt == default)
            {
                prop.CurrentValue = now;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.FirstName).HasMaxLength(100);
            e.Property(c => c.LastName).HasMaxLength(100);
            e.Property(c => c.Email).HasMaxLength(200);
            e.Property(c => c.Phone).HasMaxLength(20);
            e.Property(c => c.Persona).HasMaxLength(50);
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Bsb).HasMaxLength(7);
            e.Property(a => a.AccountNumber).HasMaxLength(10);
            e.Property(a => a.Name).HasMaxLength(100);
            e.Property(a => a.Balance).HasPrecision(18, 2);
            e.Property(a => a.LoanAmount).HasPrecision(18, 2);
            e.Property(a => a.InterestRate).HasPrecision(5, 2);
            e.Property(a => a.BonusInterestRate).HasPrecision(5, 2);

            e.HasOne(a => a.Customer)
                .WithMany(c => c.Accounts)
                .HasForeignKey(a => a.CustomerId);

            e.HasOne(a => a.HomeLoanAccount)
                .WithMany(a => a.OffsetAccounts)
                .HasForeignKey(a => a.HomeLoanAccountId);

            e.Property(a => a.RowVersion)
                .IsRowVersion();

            e.HasIndex(a => new { a.Bsb, a.AccountNumber })
                .IsUnique();
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.Property(t => t.Description).HasMaxLength(500);
            e.Property(t => t.FailureReason).HasMaxLength(500);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.OriginalCurrency).HasMaxLength(3);
            e.Property(t => t.OriginalAmount).HasPrecision(18, 2);
            e.Property(t => t.ExchangeRate).HasPrecision(18, 6);
            e.Property(t => t.FeeAmount).HasPrecision(18, 2);

            e.HasOne(t => t.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountId);

            e.HasOne(t => t.Branch)
                .WithMany()
                .HasForeignKey(t => t.BranchId)
                .IsRequired(false);

            e.HasOne(t => t.Atm)
                .WithMany()
                .HasForeignKey(t => t.AtmId)
                .IsRequired(false);

            e.HasIndex(t => t.CreatedAt);
            e.HasIndex(t => t.Status);
            e.HasIndex(t => t.TransferId);
        });

        modelBuilder.Entity<StaffUser>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Username).HasMaxLength(50);
            e.Property(s => s.DisplayName).HasMaxLength(100);
            e.Property(s => s.PasswordHash).HasMaxLength(200);
            e.Property(s => s.Role).HasMaxLength(50);
            e.HasIndex(s => s.Username).IsUnique();
        });

        modelBuilder.Entity<CustomerNote>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Content).HasMaxLength(2000);

            e.HasOne(n => n.Customer)
                .WithMany()
                .HasForeignKey(n => n.CustomerId);

            e.HasOne(n => n.StaffUser)
                .WithMany()
                .HasForeignKey(n => n.StaffUserId);

            e.HasIndex(n => n.CreatedAt);
        });

        modelBuilder.Entity<InterestAccrual>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.DailyAmount).HasPrecision(18, 6);

            e.HasOne(i => i.Account)
                .WithMany()
                .HasForeignKey(i => i.AccountId);

            e.HasOne(i => i.PostedTransaction)
                .WithMany()
                .HasForeignKey(i => i.PostedTransactionId);

            e.HasIndex(i => new { i.AccountId, i.AccrualDate }).IsUnique();
        });

        modelBuilder.Entity<SystemSettings>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Key).HasMaxLength(100);
            e.Property(s => s.Value).HasMaxLength(500);
            e.HasIndex(s => s.Key).IsUnique();
        });

        modelBuilder.Entity<AccountBalanceSnapshot>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.LedgerBalance).HasPrecision(18, 2);
            e.Property(s => s.AvailableBalance).HasPrecision(18, 2);

            e.HasOne(s => s.Account)
                .WithMany()
                .HasForeignKey(s => s.AccountId);

            e.HasIndex(s => new { s.AccountId, s.SnapshotDate }).IsUnique();
        });

        modelBuilder.Entity<ScheduledPayment>(e =>
        {
            e.HasKey(sp => sp.Id);
            e.Property(sp => sp.PayeeName).HasMaxLength(200);
            e.Property(sp => sp.PayeeBsb).HasMaxLength(7);
            e.Property(sp => sp.PayeeAccountNumber).HasMaxLength(10);
            e.Property(sp => sp.Amount).HasPrecision(18, 2);
            e.Property(sp => sp.Description).HasMaxLength(500);
            e.Property(sp => sp.Reference).HasMaxLength(200);
            e.Property(sp => sp.Frequency).HasConversion<string>().HasMaxLength(20);

            e.HasOne(sp => sp.Account)
                .WithMany()
                .HasForeignKey(sp => sp.AccountId);

            e.HasOne(sp => sp.PayeeAccount)
                .WithMany()
                .HasForeignKey(sp => sp.PayeeAccountId)
                .IsRequired(false);

            e.HasIndex(sp => new { sp.NextDueDate, sp.IsActive });
            e.HasIndex(sp => sp.AccountId);
        });

        modelBuilder.Entity<Nudge>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Message).HasMaxLength(1000);
            e.Property(n => n.Cta).HasMaxLength(100);
            e.Property(n => n.Urgency).HasConversion<string>().HasMaxLength(10);
            e.Property(n => n.Category).HasConversion<string>().HasMaxLength(20);
            e.Property(n => n.Reasoning).HasMaxLength(1000);
            e.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);

            e.HasOne(n => n.Customer)
                .WithMany()
                .HasForeignKey(n => n.CustomerId);

            e.HasIndex(n => new { n.CustomerId, n.Status });
            e.HasIndex(n => n.CreatedAt);
        });

        modelBuilder.Entity<CustomerHoliday>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Destination).HasMaxLength(100);

            e.HasOne(h => h.Customer)
                .WithMany()
                .HasForeignKey(h => h.CustomerId);

            e.HasIndex(h => new { h.CustomerId, h.StartDate, h.EndDate });
        });

        modelBuilder.Entity<Branch>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Name).HasMaxLength(100);
            e.Property(b => b.Address).HasMaxLength(200);
            e.Property(b => b.Suburb).HasMaxLength(100);
            e.Property(b => b.State).HasMaxLength(3);
            e.Property(b => b.Postcode).HasMaxLength(4);
            e.Property(b => b.Latitude).HasPrecision(9, 6);
            e.Property(b => b.Longitude).HasPrecision(9, 6);
        });

        modelBuilder.Entity<Atm>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.LocationName).HasMaxLength(200);
            e.Property(a => a.Address).HasMaxLength(200);
            e.Property(a => a.Suburb).HasMaxLength(100);
            e.Property(a => a.State).HasMaxLength(3);
            e.Property(a => a.Postcode).HasMaxLength(4);
            e.Property(a => a.Latitude).HasPrecision(9, 6);
            e.Property(a => a.Longitude).HasPrecision(9, 6);

            e.HasOne(a => a.Branch)
                .WithMany(b => b.Atms)
                .HasForeignKey(a => a.BranchId)
                .IsRequired(false);
        });
    }
}
