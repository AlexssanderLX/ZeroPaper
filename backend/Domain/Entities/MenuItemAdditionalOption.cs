using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class MenuItemAdditionalOption : TenantOwnedEntity
{
    private MenuItemAdditionalOption()
    {
    }

    public MenuItemAdditionalOption(
        Guid tenantId,
        Guid companyId,
        Guid menuItemId,
        Guid menuItemAdditionalGroupId,
        string name,
        decimal price,
        int displayOrder = 0,
        Guid? sourceMenuAdditionalCatalogOptionId = null) : base(tenantId)
    {
        CompanyId = companyId;
        MenuItemId = menuItemId;
        MenuItemAdditionalGroupId = menuItemAdditionalGroupId;
        SourceMenuAdditionalCatalogOptionId = sourceMenuAdditionalCatalogOptionId;
        Rename(name);
        UpdatePrice(price);
        SetDisplayOrder(displayOrder);
    }

    public Guid CompanyId { get; private set; }
    public Guid MenuItemId { get; private set; }
    public Guid MenuItemAdditionalGroupId { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public int DisplayOrder { get; private set; }
    public Guid? SourceMenuAdditionalCatalogOptionId { get; private set; }

    public MenuItemAdditionalGroup MenuItemAdditionalGroup { get; private set; } = null!;
    public MenuItem MenuItem { get; private set; } = null!;

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

    public void SetCatalogSource(Guid? sourceMenuAdditionalCatalogOptionId)
    {
        SourceMenuAdditionalCatalogOptionId = sourceMenuAdditionalCatalogOptionId;
        Touch();
    }
}
