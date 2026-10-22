using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Entities;
using AccountManagement.Domain.Enums;
using AccountManagement.Domain.Services;
using AccountManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountManagement.Infrastructure.Services;

public class RateService : IRateService
{
    private readonly AccountDbContext _context;
    private readonly ILogger<RateService> _logger;

    public RateService(AccountDbContext context, ILogger<RateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RatePeriodDto>> GetRatePeriodsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RatePeriods
            .Include(p => p.Rates)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Select(p => new RatePeriodDto
            {
                Id = p.Id,
                Name = p.Name,
                Year = p.Year,
                Month = p.Month,
                EffectiveStartDate = p.EffectiveStartDate,
                EffectiveEndDate = p.EffectiveEndDate,
                IsActive = p.IsActive,
                IsLocked = p.IsLocked,
                Notes = p.Notes,
                RatesCount = p.Rates.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RatePeriodDto> CreateRatePeriodAsync(string name, int year, int month, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var period = new RatePeriod
        {
            Name = name,
            Year = year,
            Month = month,
            EffectiveStartDate = startDate,
            EffectiveEndDate = endDate,
            IsActive = true,
            IsLocked = false
        };

        await _context.RatePeriods.AddAsync(period, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new RatePeriodDto
        {
            Id = period.Id,
            Name = period.Name,
            Year = period.Year,
            Month = period.Month,
            EffectiveStartDate = period.EffectiveStartDate,
            EffectiveEndDate = period.EffectiveEndDate,
            IsActive = period.IsActive,
            IsLocked = period.IsLocked,
            RatesCount = 0
        };
    }

    public async Task<IReadOnlyList<MaterialCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MaterialCategories
            .Include(c => c.Materials)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new MaterialCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                MaterialsCount = c.Materials.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MaterialDto>> GetMaterialsAsync(Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Materials
            .Include(m => m.Category)
            .AsNoTracking();

        if (categoryId.HasValue)
        {
            query = query.Where(m => m.CategoryId == categoryId.Value);
        }

        var activePeriod = await _context.RatePeriods
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .FirstOrDefaultAsync(cancellationToken);

        var rates = activePeriod != null
            ? await _context.MaterialRates
                .Where(r => r.RatePeriodId == activePeriod.Id)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
            : new List<MaterialRate>();

        var materials = await query
            .OrderBy(m => m.Category != null ? m.Category.DisplayOrder : 0)
            .ThenBy(m => m.Name)
            .Select(m => new MaterialDto
            {
                Id = m.Id,
                CategoryId = m.CategoryId,
                CategoryName = m.Category != null ? m.Category.Name : string.Empty,
                Name = m.Name,
                Code = m.Code,
                DefaultUnit = m.DefaultUnit,
                DefaultGsm = m.DefaultGsm,
                DefaultSize = m.DefaultSize,
                IsActive = m.IsActive
            })
            .ToListAsync(cancellationToken);

        foreach (var mat in materials)
        {
            var matched = rates.FirstOrDefault(r => r.MaterialId == mat.Id && (r.Size == mat.DefaultSize || r.Size == null));
            if (matched == null)
            {
                matched = rates.FirstOrDefault(r => r.MaterialId == mat.Id);
            }
            if (matched != null)
            {
                mat.CurrentEffectiveRate = matched.RatePerUnit;
            }
        }

        return materials;
    }

    public async Task<IReadOnlyList<MaterialRateDto>> GetRatesForPeriodAsync(Guid ratePeriodId, CancellationToken cancellationToken = default)
    {
        return await _context.MaterialRates
            .Include(r => r.RatePeriod)
            .Include(r => r.Material)
            .ThenInclude(m => m!.Category)
            .Where(r => r.RatePeriodId == ratePeriodId)
            .OrderBy(r => r.Material != null && r.Material.Category != null ? r.Material.Category.DisplayOrder : 0)
            .ThenBy(r => r.Material != null ? r.Material.Name : string.Empty)
            .Select(r => new MaterialRateDto
            {
                Id = r.Id,
                RatePeriodId = r.RatePeriodId,
                RatePeriodName = r.RatePeriod != null ? r.RatePeriod.Name : string.Empty,
                MaterialId = r.MaterialId,
                MaterialName = r.Material != null ? r.Material.Name : string.Empty,
                CategoryName = r.Material != null && r.Material.Category != null ? r.Material.Category.Name : string.Empty,
                Gsm = r.Gsm,
                Size = r.Size,
                MinQuantityTier = r.MinQuantityTier,
                RatePerUnit = r.RatePerUnit,
                Unit = r.Unit,
                Remarks = r.Remarks
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal?> ResolveRateAsync(
        Guid materialId,
        DateOnly transactionDate,
        string? gsm = null,
        string? size = null,
        decimal quantity = 1,
        CancellationToken cancellationToken = default)
    {
        // 1. Locate the active rate period covering the transaction date
        var period = await _context.RatePeriods
            .Where(p => p.EffectiveStartDate <= transactionDate && p.EffectiveEndDate >= transactionDate)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .FirstOrDefaultAsync(cancellationToken);

        if (period == null)
        {
            // Fall back to most recent period
            period = await _context.RatePeriods
                .OrderByDescending(p => p.EffectiveStartDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (period == null)
            return null;

        // 2. Query all rates for this material in the active period
        var rates = await _context.MaterialRates
            .Where(r => r.RatePeriodId == period.Id && r.MaterialId == materialId)
            .ToListAsync(cancellationToken);

        if (!rates.Any())
            return null;

        // Normalize inputs
        string? normSize = string.IsNullOrWhiteSpace(size)
            ? null
            : size.Trim().Replace("x", "/").Replace("X", "/").Replace(" ", "");

        string? normGsm = string.IsNullOrWhiteSpace(gsm)
            ? null
            : gsm.Trim().Replace(" ", "").ToLowerInvariant();

        // Find best match: exact size & GSM match first
        var best = rates
            .OrderByDescending(r =>
            {
                int score = 0;
                if (!string.IsNullOrWhiteSpace(normSize) && !string.IsNullOrWhiteSpace(r.Size))
                {
                    string rSize = r.Size.Trim().Replace("x", "/").Replace("X", "/").Replace(" ", "");
                    if (rSize.Equals(normSize, StringComparison.OrdinalIgnoreCase)) score += 20;
                }
                if (!string.IsNullOrWhiteSpace(normGsm) && !string.IsNullOrWhiteSpace(r.Gsm))
                {
                    string rGsm = r.Gsm.Trim().Replace(" ", "").ToLowerInvariant();
                    if (rGsm.Equals(normGsm, StringComparison.OrdinalIgnoreCase)) score += 10;
                }
                if (r.MinQuantityTier <= quantity)
                {
                    score += 5;
                }
                return score;
            })
            .ThenByDescending(r => r.MinQuantityTier)
            .FirstOrDefault();

        // If an exact size match was found, return its rate
        if (best != null && !string.IsNullOrWhiteSpace(normSize) && !string.IsNullOrWhiteSpace(best.Size))
        {
            string bestSize = best.Size.Trim().Replace("x", "/").Replace("X", "/").Replace(" ", "");
            if (bestSize.Equals(normSize, StringComparison.OrdinalIgnoreCase))
            {
                return best.RatePerUnit;
            }
        }

        // If no exact cut size rate exists in DB, but we have a base 18/23 rate,
        // use FinancialCalculator to derive the cut rate dynamically
        if (!string.IsNullOrWhiteSpace(normSize) && normSize != "18/23")
        {
            var base1000 = rates.FirstOrDefault(r => 
                (r.Size == "18/23" || r.Size == "18x23") && 
                (r.MinQuantityTier >= 1000 || r.Unit == UnitOfMeasure.Piece));

            if (base1000 != null)
            {
                return FinancialCalculator.CalculateCutSizePrice(base1000.RatePerUnit, normSize);
            }

            var baseReam = rates.FirstOrDefault(r => 
                (r.Size == "18/23" || r.Size == "18x23") && 
                r.Unit == UnitOfMeasure.Ream);

            if (baseReam != null)
            {
                // 1000 Pcs base rate = 2 Reams
                return FinancialCalculator.CalculateCutSizePrice(baseReam.RatePerUnit * 2m, normSize);
            }
        }

        return best?.RatePerUnit;
    }

    public async Task<MaterialRateDto> SaveRateAsync(CreateOrUpdateMaterialRateDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _context.MaterialRates
            .FirstOrDefaultAsync(r => r.RatePeriodId == dto.RatePeriodId &&
                                      r.MaterialId == dto.MaterialId &&
                                      r.Gsm == dto.Gsm &&
                                      r.Size == dto.Size &&
                                      r.MinQuantityTier == dto.MinQuantityTier, cancellationToken);

        if (existing != null)
        {
            existing.RatePerUnit = dto.RatePerUnit;
            existing.Unit = dto.Unit;
            existing.Remarks = dto.Remarks;
        }
        else
        {
            existing = new MaterialRate
            {
                RatePeriodId = dto.RatePeriodId,
                MaterialId = dto.MaterialId,
                Gsm = dto.Gsm,
                Size = dto.Size,
                MinQuantityTier = dto.MinQuantityTier,
                RatePerUnit = dto.RatePerUnit,
                Unit = dto.Unit,
                Remarks = dto.Remarks
            };
            await _context.MaterialRates.AddAsync(existing, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var loaded = await _context.MaterialRates
            .Include(r => r.RatePeriod)
            .Include(r => r.Material)
            .ThenInclude(m => m!.Category)
            .FirstOrDefaultAsync(r => r.Id == existing.Id, cancellationToken);

        var material = loaded?.Material ?? await _context.Materials.FindAsync(new object[] { existing.MaterialId }, cancellationToken);
        var period = loaded?.RatePeriod ?? await _context.RatePeriods.FindAsync(new object[] { existing.RatePeriodId }, cancellationToken);

        return new MaterialRateDto
        {
            Id = existing.Id,
            RatePeriodId = existing.RatePeriodId,
            RatePeriodName = period?.Name ?? string.Empty,
            MaterialId = existing.MaterialId,
            MaterialName = material?.Name ?? string.Empty,
            CategoryName = material?.Category?.Name ?? string.Empty,
            Gsm = existing.Gsm,
            Size = existing.Size,
            MinQuantityTier = existing.MinQuantityTier,
            RatePerUnit = existing.RatePerUnit,
            Unit = existing.Unit,
            Remarks = existing.Remarks
        };
    }
}
