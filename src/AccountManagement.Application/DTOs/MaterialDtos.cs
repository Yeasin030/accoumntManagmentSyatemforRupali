using AccountManagement.Domain.Enums;

namespace AccountManagement.Application.DTOs;

public class MaterialCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int MaterialsCount { get; set; }
}

public class MaterialDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public UnitOfMeasure DefaultUnit { get; set; }
    public string? DefaultGsm { get; set; }
    public string? DefaultSize { get; set; }
    public bool IsActive { get; set; }
    public decimal? CurrentEffectiveRate { get; set; }
}

public class RatePeriodDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public DateOnly EffectiveStartDate { get; set; }
    public DateOnly EffectiveEndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public string? Notes { get; set; }
    public int RatesCount { get; set; }
}

public class MaterialRateDto
{
    public Guid Id { get; set; }
    public Guid RatePeriodId { get; set; }
    public string RatePeriodName { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Gsm { get; set; }
    public string? Size { get; set; }
    public decimal MinQuantityTier { get; set; }
    public decimal RatePerUnit { get; set; }
    public UnitOfMeasure Unit { get; set; }
    public string? Remarks { get; set; }
}

public class CreateOrUpdateMaterialRateDto
{
    public Guid RatePeriodId { get; set; }
    public Guid MaterialId { get; set; }
    public string? Gsm { get; set; }
    public string? Size { get; set; }
    public decimal MinQuantityTier { get; set; } = 1m;
    public decimal RatePerUnit { get; set; }
    public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.Ream;
    public string? Remarks { get; set; }
}
