using AccountManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<MaterialCategory> MaterialCategories { get; }
    DbSet<Material> Materials { get; }
    DbSet<RatePeriod> RatePeriods { get; }
    DbSet<MaterialRate> MaterialRates { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<TransactionItem> TransactionItems { get; }
    DbSet<CostEntry> CostEntries { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<AppUser> Users { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<AppSetting> AppSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
