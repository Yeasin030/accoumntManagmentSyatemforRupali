using AccountManagement.Domain.Common;

namespace AccountManagement.Domain.Entities;

public class RatePeriod : BaseEntity
{
    public string Name { get; set; } = string.Empty; // e.g., "October 2026"
    public int Year { get; set; }
    public int Month { get; set; }
    public DateOnly EffectiveStartDate { get; set; }
    public DateOnly EffectiveEndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; } = false; // When locked, historical rates cannot be casually altered
    public string? Notes { get; set; }

    public ICollection<MaterialRate> Rates { get; set; } = new List<MaterialRate>();
}
