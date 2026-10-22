using AccountManagement.Domain.Common;

namespace AccountManagement.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal OpeningBalance { get; set; } = 0m;
    public decimal CurrentBalance { get; set; } = 0m;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
