using AccountManagement.Domain.Common;
using AccountManagement.Domain.Enums;

namespace AccountManagement.Domain.Entities;

public class CostEntry : BaseEntity
{
    public Guid TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public CostCategory Category { get; set; } = CostCategory.Miscellaneous;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
