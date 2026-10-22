using AccountManagement.Domain.Common;
using AccountManagement.Domain.Enums;

namespace AccountManagement.Domain.Entities;

public class Expense : BaseEntity
{
    public string ExpenseNumber { get; set; } = string.Empty;
    public DateOnly ExpenseDate { get; set; }
    public string Category { get; set; } = string.Empty; // Rent, Electricity, Machine Maintenance, Ink, Salaries, etc.
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? ReferenceNumber { get; set; }
    public string? PaidTo { get; set; }
    public string? Remarks { get; set; }
}
