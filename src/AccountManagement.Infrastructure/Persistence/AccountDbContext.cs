using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Domain.Entities;
using AccountManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Infrastructure.Persistence;

public class AccountDbContext : DbContext, IApplicationDbContext
{
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options)
    {
    }

    public DbSet<MaterialCategory> MaterialCategories => Set<MaterialCategory>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<RatePeriod> RatePeriods => Set<RatePeriod>();
    public DbSet<MaterialRate> MaterialRates => Set<MaterialRate>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionItem> TransactionItems => Set<TransactionItem>();
    public DbSet<CostEntry> CostEntries => Set<CostEntry>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global decimal precision for financial integrity
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        // Transaction Configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TransactionNumber).IsUnique();
            entity.HasIndex(e => e.TransactionDate);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.TransactionNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.WorkDescription).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CancellationReason).HasMaxLength(250);

            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Transactions)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Items)
                  .WithOne(i => i.Transaction)
                  .HasForeignKey(i => i.TransactionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.CostEntries)
                  .WithOne(c => c.Transaction)
                  .HasForeignKey(c => c.TransactionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Payments)
                  .WithOne(p => p.Transaction)
                  .HasForeignKey(p => p.TransactionId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // TransactionItem Configuration
        modelBuilder.Entity<TransactionItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MaterialNameSnapshot).HasMaxLength(150).IsRequired();
            entity.Property(e => e.GsmSnapshot).HasMaxLength(50);
            entity.Property(e => e.SizeSnapshot).HasMaxLength(50);

            entity.HasOne(e => e.Material)
                  .WithMany(m => m.TransactionItems)
                  .HasForeignKey(e => e.MaterialId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Customer Configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(25);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.PhoneNumber);
        });

        // Payment Configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PaymentNumber).IsUnique();
            entity.HasIndex(e => e.PaymentDate);
            entity.Property(e => e.PaymentNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ReferenceNumber).HasMaxLength(100);

            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Payments)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Expense Configuration
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ExpenseNumber).IsUnique();
            entity.HasIndex(e => e.ExpenseDate);
            entity.Property(e => e.ExpenseNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
        });

        // MaterialCategory Configuration
        modelBuilder.Entity<MaterialCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Material Configuration
        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.HasIndex(e => new { e.CategoryId, e.Name }).IsUnique();

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Materials)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // RatePeriod Configuration
        modelBuilder.Entity<RatePeriod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => new { e.Year, e.Month }).IsUnique();
        });

        // MaterialRate Configuration
        modelBuilder.Entity<MaterialRate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.RatePeriod)
                  .WithMany(p => p.Rates)
                  .HasForeignKey(e => e.RatePeriodId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Material)
                  .WithMany(m => m.Rates)
                  .HasForeignKey(e => e.MaterialId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // AppSetting Configuration
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(100);
        });

        // AppUser Configuration
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
        });
    }
}
