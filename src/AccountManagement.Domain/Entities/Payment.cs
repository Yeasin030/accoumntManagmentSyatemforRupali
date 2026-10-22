using AccountManagement.Domain.Common;
using AccountManagement.Domain.Enums;

namespace AccountManagement.Domain.Entities;

public class Payment : BaseEntity
{
    public string PaymentNumber { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? ReferenceNumber { get; set; } // e.g., bKash TrxID, Bank Cheque No
    public string? Remarks { get; set; }
}
