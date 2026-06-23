using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class MenuAdditionalCatalogOption : TenantOwnedEntity
{
    private MenuAdditionalCatalogOption()
    {
    }

    public MenuAdditionalCatalogOption(
        Guid tenantId,
        Guid companyId,
        Guid menuAdditionalCatalogGroupId,
        string name,
        decimal price,
        int displayOrder = 0) : base(tenantId)
    {
        CompanyId = companyId;
        MenuAdditionalCatalogGroupId = menuAdditionalCatalogGroupId;
        Rename(name);
        UpdatePrice(price);
        SetDisplayOrder(displayOrder);
    }

    public Guid CompanyId { get; private set; }
    public Guid MenuAdditionalCatalogGroupId { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public int DisplayOrder { get; private set; }

    public MenuAdditionalCatalogGroup MenuAdditionalCatalogGroup { get; private set; } = null!;

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Touch();
    }

    public void UpdatePrice(decimal price)
    {
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Additional price cannot be negative.");
        }

        Price = price;
        Touch();
    }

    public void SetDisplayOrder(int displayOrder)
    {
        DisplayOrder = Math.Max(0, displayOrder);
        Touch();
    }
}
