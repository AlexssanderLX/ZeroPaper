using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class MenuCategory : TenantOwnedEntity
{
    private readonly List<MenuItem> _items = [];

    private MenuCategory()
    {
    }

    public MenuCategory(Guid tenantId, Guid companyId, string name, int displayOrder = 0, string? imageUrl = null) : base(tenantId)
    {
        CompanyId = companyId;
        Rename(name);
        SetDisplayOrder(displayOrder);
        UpdateImage(imageUrl);
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? ImageUrl { get; private set; }
    public int DisplayOrder { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
    public IReadOnlyCollection<MenuItem> Items => _items.AsReadOnly();

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Touch();
    }

    public void SetDisplayOrder(int displayOrder)
    {
        DisplayOrder = Math.Max(0, displayOrder);
        Touch();
    }

    public void UpdateImage(string? imageUrl)
    {
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        Touch();
    }
}
