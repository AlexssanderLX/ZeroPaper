using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class MenuCategory : TenantOwnedEntity
{
    private readonly List<MenuItem> _items = [];

    private MenuCategory()
    {
    }

    public MenuCategory(Guid tenantId, Guid companyId, string name, int displayOrder = 0) : base(tenantId)
    {
        CompanyId = companyId;
        Rename(name);
        SetDisplayOrder(displayOrder);
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
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
}
