using System.Security.Cryptography;
using System.Text.Json;
using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Domain.Entities;
using AccountManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountManagement.Infrastructure.Services;

public class PostgreSqlBackupService : IBackupService
{
    private readonly AccountDbContext _context;
    private readonly ILogger<PostgreSqlBackupService> _logger;

    public PostgreSqlBackupService(AccountDbContext context, ILogger<PostgreSqlBackupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupFileName = $"RupaliAccount_Backup_{timestamp}.json";
        string backupFilePath = Path.Combine(destinationDirectory, backupFileName);

        _logger.LogInformation("Initiating database backup to {FilePath}", backupFilePath);

        var dataBundle = new DatabaseBackupBundle
        {
            Version = "1.0",
            CreatedAt = DateTime.UtcNow,
            Categories = await _context.MaterialCategories.AsNoTracking().ToListAsync(cancellationToken),
            Materials = await _context.Materials.AsNoTracking().ToListAsync(cancellationToken),
            RatePeriods = await _context.RatePeriods.AsNoTracking().ToListAsync(cancellationToken),
            Rates = await _context.MaterialRates.AsNoTracking().ToListAsync(cancellationToken),
            Customers = await _context.Customers.AsNoTracking().ToListAsync(cancellationToken),
            Transactions = await _context.Transactions
                .Include(t => t.Items)
                .Include(t => t.CostEntries)
                .AsNoTracking().ToListAsync(cancellationToken),
            Payments = await _context.Payments.AsNoTracking().ToListAsync(cancellationToken),
            Expenses = await _context.Expenses.AsNoTracking().ToListAsync(cancellationToken),
            AppSettings = await _context.AppSettings.AsNoTracking().ToListAsync(cancellationToken)
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonContent = JsonSerializer.Serialize(dataBundle, options);

        await File.WriteAllTextAsync(backupFilePath, jsonContent, cancellationToken);

        // Verification: ensure file exists, has size > 0, and can be read back
        var fileInfo = new FileInfo(backupFilePath);
        if (!fileInfo.Exists || fileInfo.Length == 0)
        {
            throw new IOException($"Backup verification failed: File {backupFilePath} is empty or missing.");
        }

        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(backupFilePath);
        byte[] hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        string hashStr = Convert.ToHexString(hash);

        // Write checksum file
        string checksumPath = $"{backupFilePath}.sha256";
        await File.WriteAllTextAsync(checksumPath, hashStr, cancellationToken);

        _logger.LogInformation("Database backup verified successfully. File size: {Size} bytes, SHA256: {Hash}",
            fileInfo.Length, hashStr);

        return backupFilePath;
    }

    public async Task<bool> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupFilePath))
        {
            _logger.LogError("Backup file not found at: {FilePath}", backupFilePath);
            return false;
        }

        // Verify SHA256 if .sha256 file exists
        string checksumPath = $"{backupFilePath}.sha256";
        if (File.Exists(checksumPath))
        {
            string expectedHash = (await File.ReadAllTextAsync(checksumPath, cancellationToken)).Trim();
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(backupFilePath);
            byte[] actualHashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
            string actualHash = Convert.ToHexString(actualHashBytes);

            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Backup file checksum mismatch! The file may be corrupt or modified.");
            }
        }

        string jsonContent = await File.ReadAllTextAsync(backupFilePath, cancellationToken);
        var bundle = JsonSerializer.Deserialize<DatabaseBackupBundle>(jsonContent);

        if (bundle == null)
        {
            throw new InvalidOperationException("Failed to parse backup archive content.");
        }

        _logger.LogWarning("Restoring database from backup dated {BackupDate}...", bundle.CreatedAt);

        // Begin transaction to guarantee all-or-nothing restoration
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Clear existing transaction items & costs
            _context.TransactionItems.RemoveRange(await _context.TransactionItems.ToListAsync(cancellationToken));
            _context.CostEntries.RemoveRange(await _context.CostEntries.ToListAsync(cancellationToken));
            _context.Payments.RemoveRange(await _context.Payments.ToListAsync(cancellationToken));
            _context.Transactions.RemoveRange(await _context.Transactions.ToListAsync(cancellationToken));
            _context.Expenses.RemoveRange(await _context.Expenses.ToListAsync(cancellationToken));
            _context.MaterialRates.RemoveRange(await _context.MaterialRates.ToListAsync(cancellationToken));
            _context.RatePeriods.RemoveRange(await _context.RatePeriods.ToListAsync(cancellationToken));
            _context.Materials.RemoveRange(await _context.Materials.ToListAsync(cancellationToken));
            _context.MaterialCategories.RemoveRange(await _context.MaterialCategories.ToListAsync(cancellationToken));
            _context.Customers.RemoveRange(await _context.Customers.ToListAsync(cancellationToken));

            await _context.SaveChangesAsync(cancellationToken);

            // Re-populate from backup
            await _context.MaterialCategories.AddRangeAsync(bundle.Categories, cancellationToken);
            await _context.Materials.AddRangeAsync(bundle.Materials, cancellationToken);
            await _context.RatePeriods.AddRangeAsync(bundle.RatePeriods, cancellationToken);
            await _context.MaterialRates.AddRangeAsync(bundle.Rates, cancellationToken);
            await _context.Customers.AddRangeAsync(bundle.Customers, cancellationToken);
            await _context.Transactions.AddRangeAsync(bundle.Transactions, cancellationToken);
            await _context.Payments.AddRangeAsync(bundle.Payments, cancellationToken);
            await _context.Expenses.AddRangeAsync(bundle.Expenses, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Database restored successfully from backup.");
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Database restoration failed. Rolled back all changes.");
            throw;
        }
    }
}

public class DatabaseBackupBundle
{
    public string Version { get; set; } = "1.0";
    public DateTime CreatedAt { get; set; }
    public List<MaterialCategory> Categories { get; set; } = new();
    public List<Material> Materials { get; set; } = new();
    public List<RatePeriod> RatePeriods { get; set; } = new();
    public List<MaterialRate> Rates { get; set; } = new();
    public List<Customer> Customers { get; set; } = new();
    public List<Transaction> Transactions { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
    public List<Expense> Expenses { get; set; } = new();
    public List<AppSetting> AppSettings { get; set; } = new();
}
