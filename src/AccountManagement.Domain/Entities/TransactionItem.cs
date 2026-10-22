using AccountManagement.Domain.Common;
using AccountManagement.Domain.Enums;

namespace AccountManagement.Domain.Entities;

public class TransactionItem : BaseEntity
{
    public Guid TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public Guid? MaterialId { get; set; }
    public Material? Material { get; set; }

    // Rate & Specification Snapshots (Permanent record, never altered by future rate updates)
    public string MaterialNameSnapshot { get; set; } = string.Empty;
    public string? GsmSnapshot { get; set; }
    public string? SizeSnapshot { get; set; }
    public decimal UnitRateSnapshot { get; set; }
    
    public decimal Quantity { get; set; }
    public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.Ream;
    public decimal TotalMaterialCost { get; set; }

    public void CalculateCost()
    {
        TotalMaterialCost = Math.Round(Quantity * UnitRateSnapshot, 2, MidpointRounding.AwayFromZero);
    }
}
