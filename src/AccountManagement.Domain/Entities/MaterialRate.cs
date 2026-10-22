using AccountManagement.Domain.Common;
using AccountManagement.Domain.Enums;

namespace AccountManagement.Domain.Entities;

public class MaterialRate : BaseEntity
{
    public Guid RatePeriodId { get; set; }
    public RatePeriod? RatePeriod { get; set; }

    public Guid MaterialId { get; set; }
    public Material? Material { get; set; }

    public string? Gsm { get; set; }
    public string? Size { get; set; }
    public decimal MinQuantityTier { get; set; } = 1m; // Minimum quantity to qualify for this rate
    public decimal RatePerUnit { get; set; }
    public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.Ream;
    public string? Remarks { get; set; }
}
