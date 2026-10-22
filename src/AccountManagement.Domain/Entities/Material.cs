using AccountManagement.Domain.Common;
using AccountManagement.Domain.Enums;

namespace AccountManagement.Domain.Entities;

public class Material : BaseEntity
{
    public Guid CategoryId { get; set; }
    public MaterialCategory? Category { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public UnitOfMeasure DefaultUnit { get; set; } = UnitOfMeasure.Ream;
    public string? DefaultGsm { get; set; }
    public string? DefaultSize { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }

    public ICollection<MaterialRate> Rates { get; set; } = new List<MaterialRate>();
    public ICollection<TransactionItem> TransactionItems { get; set; } = new List<TransactionItem>();
}
