using AccountManagement.Domain.Common;
using AccountManagement.Domain.Enums;

namespace AccountManagement.Domain.Entities;

public class Transaction : BaseEntity
{
    public string TransactionNumber { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string WorkDescription { get; set; } = string.Empty;
    
    // Financials
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalBillAmount { get; set; }
    public decimal TotalDirectCost { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal BalanceDue { get; set; }

    // Status and Audit Safeguards
    public TransactionStatus Status { get; set; } = TransactionStatus.Active;
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? Notes { get; set; }

    // Child Collections
    public ICollection<TransactionItem> Items { get; set; } = new List<TransactionItem>();
    public ICollection<CostEntry> CostEntries { get; set; } = new List<CostEntry>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public void RecalculateTotals()
    {
        decimal paperCost = Items.Sum(i => i.TotalMaterialCost);
        decimal productionCost = CostEntries.Sum(c => c.Amount);
        
        TotalDirectCost = paperCost + productionCost;
        
        // If subtotal is not manually overridden, it defaults to paper + costs
        if (SubTotal == 0 && (Items.Count > 0 || CostEntries.Count > 0))
        {
            SubTotal = paperCost + productionCost;
        }

        TotalBillAmount = Math.Max(0m, SubTotal - Discount);
        TotalPaidAmount = Payments.Sum(p => p.Amount);
        BalanceDue = Math.Max(0m, TotalBillAmount - TotalPaidAmount);

        if (Status != TransactionStatus.Cancelled)
        {
            Status = BalanceDue <= 0m ? TransactionStatus.Completed : TransactionStatus.Active;
        }
    }

    public void Cancel(string reason)
    {
        Status = TransactionStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = DateTime.UtcNow;
    }
}
