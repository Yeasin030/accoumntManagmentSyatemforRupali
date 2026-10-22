using AccountManagement.Domain.Common;

namespace AccountManagement.Domain.Entities;

public class AppUser : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string Role { get; set; } = "Administrator"; // Owner/Administrator for single-user
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
}
