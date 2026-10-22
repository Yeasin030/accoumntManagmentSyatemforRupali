using AccountManagement.Domain.Enums;

namespace AccountManagement.Application.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }

    public string WorkDescription { get; set; } = string.Empty;
    
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalBillAmount { get; set; }
    public decimal TotalDirectCost { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal BalanceDue { get; set; }

    // Direct Cost & Profit Calculations
    public decimal Profit => TotalBillAmount - TotalDirectCost;
    public decimal ProfitMargin => TotalBillAmount > 0 ? Math.Round((Profit / TotalBillAmount) * 100, 1) : 0m;

    public TransactionStatus Status { get; set; }
    public bool IsActive => Status == TransactionStatus.Active;
    public bool IsCancelled => Status == TransactionStatus.Cancelled;
    public bool CanCancel => Status != TransactionStatus.Cancelled;
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? Notes { get; set; }

    public List<TransactionItemDto> Items { get; set; } = new();
    public List<CostEntryDto> CostEntries { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
}

public class TransactionItemDto
{
    public Guid Id { get; set; }
    public Guid? MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string? Gsm { get; set; }
    public string? Size { get; set; }
    public decimal Quantity { get; set; }
    public UnitOfMeasure Unit { get; set; }
    public decimal UnitRate { get; set; }
    public decimal TotalMaterialCost { get; set; }
}

public class CostEntryDto
{
    public Guid Id { get; set; }
    public CostCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class CreateTransactionDto
{
    public DateOnly TransactionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public Guid CustomerId { get; set; }
    public string WorkDescription { get; set; } = string.Empty;
    public decimal? SubTotal { get; set; } // Customer agreed work price (before discount)
    public decimal Discount { get; set; } = 0m;
    public string? Notes { get; set; }

    public List<CreateTransactionItemDto> Items { get; set; } = new();
    public List<CreateCostEntryDto> CostEntries { get; set; } = new();
    
    // Optional initial payment / advance paid at job creation
    public decimal? InitialPaymentAmount { get; set; }
    public PaymentMethod? InitialPaymentMethod { get; set; }
    public string? InitialPaymentReference { get; set; }
}

public class CreateTransactionItemDto
{
    public Guid? MaterialId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public string? Gsm { get; set; }
    public string? Size { get; set; }
    public decimal Quantity { get; set; }
    public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.Ream;
    public decimal UnitRate { get; set; }
}

public class CreateCostEntryDto
{
    public CostCategory Category { get; set; } = CostCategory.Miscellaneous;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
