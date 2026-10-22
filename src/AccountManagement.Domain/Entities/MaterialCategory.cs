using AccountManagement.Domain.Common;

namespace AccountManagement.Domain.Entities;

public class MaterialCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Material> Materials { get; set; } = new List<Material>();
}
