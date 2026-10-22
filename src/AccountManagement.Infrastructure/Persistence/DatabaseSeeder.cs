using System.Security.Cryptography;
using System.Text;
using AccountManagement.Domain.Entities;
using AccountManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountManagement.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly AccountDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(AccountDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Seed Categories and Materials
            _logger.LogInformation("Ensuring default material categories and materials are seeded...");
            await SeedCategoriesAndMaterialsAsync(cancellationToken);

            // Seed Single User / Owner Account
            if (!await _context.Users.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("Seeding default single-user account...");
                await SeedDefaultUserAsync(cancellationToken);
            }

            // Seed App Settings
            if (!await _context.AppSettings.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("Seeding default application settings...");
                await SeedSettingsAsync(cancellationToken);
            }

            // Seed initial Rate Period & June rates
            _logger.LogInformation("Ensuring June rate card and cut rates are seeded...");
            await SeedInitialRatePeriodAndRatesAsync(cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task SeedCategoriesAndMaterialsAsync(CancellationToken cancellationToken)
    {
        var categoryDefs = new List<(string Name, string Description, int Order, List<(string Name, UnitOfMeasure Unit, string? Gsm, string? Size)> Materials)>
        {
            ("Art Paper", "High quality coated paper for flyers, brochures, posters", 1, new()
            {
                ("Art Paper 80 GSM", UnitOfMeasure.Ream, "80 GSM", "18x23"),
                ("Art Paper 100 GSM", UnitOfMeasure.Ream, "100 GSM", "18x23"),
                ("Art Paper 120 GSM", UnitOfMeasure.Ream, "120 GSM", "18x23"),
                ("Art Paper 150 GSM", UnitOfMeasure.Ream, "150 GSM", "18x23")
            }),
            ("Offset Paper", "Uncoated woodfree paper for books, stationary, memo pads", 2, new()
            {
                ("Offset Paper 55 GSM", UnitOfMeasure.Ream, "55 GSM", "18x23"),
                ("Offset Paper 61 GSM", UnitOfMeasure.Ream, "61 GSM", "18x23"),
                ("Offset Paper 65 GSM", UnitOfMeasure.Ream, "65 GSM", "18x23"),
                ("Offset Paper 70 GSM", UnitOfMeasure.Ream, "70 GSM", "18x23"),
                ("Offset Paper 80 GSM", UnitOfMeasure.Ream, "80 GSM", "18x23"),
                ("Offset Paper 100 GSM", UnitOfMeasure.Ream, "100 GSM", "18x23"),
                ("Offset Paper 120 GSM", UnitOfMeasure.Ream, "120 GSM", "18x23"),
                ("News Paper", UnitOfMeasure.Ream, "45 GSM", "18x23"),
                ("Colour Paper", UnitOfMeasure.Ream, "70 GSM", "18x23")
            }),
            ("Auto Carbon", "Carbonless copy paper (NCR) for invoices, receipts, challans", 3, new()
            {
                ("Bashundhara Carbon", UnitOfMeasure.Ream, "55 GSM", "18x23"),
                ("China Carbon", UnitOfMeasure.Ream, "55 GSM", "18x23")
            }),
            ("Art Card", "Heavyweight coated board for packaging boxes, cards, covers", 4, new()
            {
                ("300 GSM Art Card Hansol", UnitOfMeasure.Sheet, "300 GSM", "22x28"),
                ("300 GSM Art Card Nimbo", UnitOfMeasure.Sheet, "300 GSM", "22x28"),
                ("300 GSM Art Card Hi-Kote", UnitOfMeasure.Sheet, "300 GSM", "22x28")
            }),
            ("Colour Card", "Colored cover paper & cardboards", 5, new()
            {
                ("160 GSM Colour Card", UnitOfMeasure.Sheet, "160 GSM", "22x28"),
                ("175 GSM Colour Card", UnitOfMeasure.Sheet, "175 GSM", "22x28"),
                ("240 GSM Colour Card", UnitOfMeasure.Sheet, "240 GSM", "22x28")
            }),
            ("Sticker", "Self-adhesive paper & film for labels and packaging", 6, new()
            {
                ("P.S-1 Sticker", UnitOfMeasure.Sheet, "80 GSM", "20x30")
            }),
            ("Ledger Paper", "Special archival paper for account books & legal registers", 7, new()
            {
                ("80 GSM Ledger Paper", UnitOfMeasure.Ream, "80 GSM", "20x30")
            })
        };

        var existingCategories = await _context.MaterialCategories
            .Include(c => c.Materials)
            .ToListAsync(cancellationToken);

        foreach (var (catName, catDesc, order, materials) in categoryDefs)
        {
            var category = existingCategories.FirstOrDefault(c => c.Name == catName);
            if (category == null)
            {
                category = new MaterialCategory
                {
                    Name = catName,
                    Description = catDesc,
                    DisplayOrder = order,
                    IsActive = true
                };
                await _context.MaterialCategories.AddAsync(category, cancellationToken);
                existingCategories.Add(category);
            }

            foreach (var (matName, unit, gsm, size) in materials)
            {
                var existingMaterial = category.Materials.FirstOrDefault(m => m.Name == matName);
                if (existingMaterial == null)
                {
                    category.Materials.Add(new Material
                    {
                        Name = matName,
                        DefaultUnit = unit,
                        DefaultGsm = gsm,
                        DefaultSize = size,
                        IsActive = true
                    });
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDefaultUserAsync(CancellationToken cancellationToken)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
        string salt = Convert.ToBase64String(saltBytes);

        // Default password: admin (can be changed in settings)
        using var pbkdf2 = new Rfc2898DeriveBytes("admin", saltBytes, 10000, HashAlgorithmName.SHA256);
        string hash = Convert.ToBase64String(pbkdf2.GetBytes(32));

        var user = new AppUser
        {
            Username = "admin",
            FullName = "Account Owner",
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = "Administrator",
            IsActive = true
        };

        await _context.Users.AddAsync(user, cancellationToken);
    }

    private async Task SeedSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = new List<AppSetting>
        {
            new() { Key = "BusinessName", Value = "Rupali Press & Packaging", Category = "General", Description = "Business / Organization Name" },
            new() { Key = "Address", Value = "Dhaka, Bangladesh", Category = "General", Description = "Business Address" },
            new() { Key = "Phone", Value = "+880 1700-000000", Category = "General", Description = "Contact Phone Number" },
            new() { Key = "CurrencySymbol", Value = "৳", Category = "Financial", Description = "Currency Symbol" },
            new() { Key = "CurrencyCode", Value = "BDT", Category = "Financial", Description = "Currency ISO Code" },
            new() { Key = "Language", Value = "bn", Category = "General", Description = "Default Language (bn or en)" }
        };

        await _context.AppSettings.AddRangeAsync(settings, cancellationToken);
    }

    private async Task SeedInitialRatePeriodAndRatesAsync(CancellationToken cancellationToken)
    {
        var period = await _context.RatePeriods.FirstOrDefaultAsync(p => p.IsActive, cancellationToken);
        if (period == null)
        {
            var now = DateTime.UtcNow;
            var startDate = new DateOnly(now.Year, now.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            period = new RatePeriod
            {
                Name = "June Rates | জুন মাসের রেট",
                Year = now.Year,
                Month = now.Month,
                EffectiveStartDate = startDate,
                EffectiveEndDate = endDate,
                IsActive = true,
                IsLocked = false,
                Notes = "Standard June Month printing press rate card (কাগজ, প্লেট, ছাপা, বাইন্ডিং)"
            };

            await _context.RatePeriods.AddAsync(period, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Fetch all materials and existing rates
        var materials = await _context.Materials.ToListAsync(cancellationToken);
        var existingRates = await _context.MaterialRates
            .Where(r => r.RatePeriodId == period.Id)
            .ToListAsync(cancellationToken);

        // List of all paper items with Base Rate for 18/23 (1000 Pcs / 2 Reams)
        var paperRates = new List<(string Name, decimal BaseRate)>
        {
            ("Art Paper 150 GSM", 6000m),
            ("Art Paper 120 GSM", 4650m),
            ("Art Paper 100 GSM", 3950m),
            ("Art Paper 80 GSM", 3420m),

            ("Offset Paper 120 GSM", 4880m),
            ("Offset Paper 100 GSM", 3960m),
            ("Offset Paper 80 GSM", 3140m),
            ("Offset Paper 70 GSM", 2780m),
            ("Offset Paper 65 GSM", 2620m),
            ("Offset Paper 61 GSM", 2500m),
            ("Offset Paper 55 GSM", 2150m),
            ("News Paper", 1100m),
            ("Colour Paper", 2300m),

            ("Bashundhara Carbon", 3000m),
            ("China Carbon", 3650m)
        };

        // Standard cut sizes: (Size, Unit, Divisor, Tier, Remarks)
        foreach (var (matName, baseRate) in paperRates)
        {
            var mat = materials.FirstOrDefault(m => m.Name.Equals(matName, StringComparison.OrdinalIgnoreCase));
            if (mat == null) continue;

            var sizeVariants = new List<(string Size, UnitOfMeasure Unit, decimal Rate, decimal Tier, string Remarks)>
            {
                // Ream rate (1 Ream = 500 sheets = half of 1000 pcs)
                ("18/23", UnitOfMeasure.Ream, Math.Round(baseRate / 2m, 2), 1m, "Per Ream (500 sheets)"),
                // 18/23: 1000 Pcs base rate
                ("18/23", UnitOfMeasure.Piece, baseRate, 1000m, "1000 Pcs (2 Reams) Base Demy"),
                // 9/14: 3 cuts with trim (divisor 2.95). 80 GSM Offset is 1064 (correcting typo 106)
                ("9/14", UnitOfMeasure.Piece, Math.Round(baseRate / 2.95m, 0), 1000m, "1000 Pcs (3-cut with trim)"),
                // 9/11.5: 4 cuts exact (divisor 4.0).
                ("9/11.5", UnitOfMeasure.Piece, Math.Round(baseRate / 4.0m, 2), 1000m, "1000 Pcs (4-cut / Quarter Demy)"),
                // 7.5/10: 5 cuts (divisor 5.0, or 4.90-4.95 with trim). 120 GSM Offset formatted to 996
                ("7.5/10", UnitOfMeasure.Piece, Math.Round(baseRate / 4.95m, 0), 1000m, "1000 Pcs (5-cut)"),
                // 7.5/9: 6 cuts with trim (divisor 5.9)
                ("7.5/9", UnitOfMeasure.Piece, Math.Round(baseRate / 5.9m, 0), 1000m, "1000 Pcs (6-cut with trim)")
            };

            foreach (var (sz, u, rate, tier, remarks) in sizeVariants)
            {
                UpsertRate(existingRates, period.Id, mat.Id, mat.DefaultGsm, sz, rate, u, tier, remarks);
            }
        }

        // Art Cards: Per Sheet (22/28)
        var cardRates = new List<(string Name, decimal Rate)>
        {
            ("300 GSM Art Card Hansol", 21m),
            ("300 GSM Art Card Nimbo", 15m),
            ("300 GSM Art Card Hi-Kote", 15m),
            ("240 GSM Colour Card", 20m),
            ("175 GSM Colour Card", 17m),
            ("160 GSM Colour Card", 16m)
        };

        foreach (var (cName, rate) in cardRates)
        {
            var mat = materials.FirstOrDefault(m => m.Name.Equals(cName, StringComparison.OrdinalIgnoreCase));
            if (mat != null)
            {
                UpsertRate(existingRates, period.Id, mat.Id, mat.DefaultGsm, "22/28", rate, UnitOfMeasure.Sheet, 1m, "Per Sheet / প্রতি তা");
            }
        }

        // Sticker: P.S-1 (20/30) 17.50 / Sheet
        var sticker = materials.FirstOrDefault(m => m.Name.Contains("P.S-1 Sticker", StringComparison.OrdinalIgnoreCase));
        if (sticker != null)
        {
            UpsertRate(existingRates, period.Id, sticker.Id, sticker.DefaultGsm, "20/30", 17.50m, UnitOfMeasure.Sheet, 1m, "Per Sheet / প্রতি তা");
        }

        // Ledger Paper: 80 GSM (20/30) 3100 for 1000 Pcs / 1550 per Ream
        var ledger = materials.FirstOrDefault(m => m.Name.Contains("Ledger Paper", StringComparison.OrdinalIgnoreCase));
        if (ledger != null)
        {
            UpsertRate(existingRates, period.Id, ledger.Id, "80 GSM", "20/30", 1550m, UnitOfMeasure.Ream, 1m, "Per Ream (3100 per 1000 pcs / 2 Reams)");
            UpsertRate(existingRates, period.Id, ledger.Id, "80 GSM", "20/30", 3100m, UnitOfMeasure.Piece, 1000m, "1000 Pcs (2 Reams)");
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private void UpsertRate(
        List<MaterialRate> existingRates,
        Guid periodId,
        Guid materialId,
        string? gsm,
        string size,
        decimal rate,
        UnitOfMeasure unit,
        decimal minTier,
        string remarks)
    {
        var existing = existingRates
            .FirstOrDefault(r => r.RatePeriodId == periodId &&
                                 r.MaterialId == materialId &&
                                 r.Size == size &&
                                 r.Unit == unit &&
                                 r.MinQuantityTier == minTier);

        if (existing != null)
        {
            existing.RatePerUnit = rate;
            existing.Remarks = remarks;
        }
        else
        {
            var newRate = new MaterialRate
            {
                RatePeriodId = periodId,
                MaterialId = materialId,
                Gsm = gsm,
                Size = size,
                RatePerUnit = rate,
                Unit = unit,
                MinQuantityTier = minTier,
                Remarks = remarks
            };
            existingRates.Add(newRate);
            _context.MaterialRates.Add(newRate);
        }
    }
}
