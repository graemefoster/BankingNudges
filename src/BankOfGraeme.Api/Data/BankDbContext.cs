using Microsoft.EntityFrameworkCore;
using BankOfGraeme.Api.Models;

namespace BankOfGraeme.Api.Data;

public class BankDbContext(DbContextOptions<BankDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<StaffUser> StaffUsers => Set<StaffUser>();
    public DbSet<CustomerNote> CustomerNotes => Set<CustomerNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.FirstName).HasMaxLength(100);
            e.Property(c => c.LastName).HasMaxLength(100);
            e.Property(c => c.Email).HasMaxLength(200);
            e.Property(c => c.Phone).HasMaxLength(20);
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
            e.Property(t => t.BalanceAfter).HasPrecision(18, 2);
            e.Property(t => t.Description).HasMaxLength(500);

            e.HasOne(t => t.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountId);

            e.HasIndex(t => t.CreatedAt);
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
    }
}
