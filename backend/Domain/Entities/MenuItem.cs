using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class MenuItem : TenantOwnedEntity
{
    private MenuItem()
    {
    }

    public MenuItem(
        Guid tenantId,
        Guid companyId,
        Guid menuCategoryId,
        string name,
        decimal price,
        string? description = null,
        string? accentLabel = null,
        string? imageUrl = null,
        int displayOrder = 0) : base(tenantId)
    {
        CompanyId = companyId;
        MenuCategoryId = menuCategoryId;
        UpdateCatalog(name, description, accentLabel, imageUrl);
        UpdatePrice(price);
        SetDisplayOrder(displayOrder);
    }

    public Guid CompanyId { get; private set; }
    public Guid MenuCategoryId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? AccentLabel { get; private set; }
    public string? ImageUrl { get; private set; }
    public decimal Price { get; private set; }
    public int DisplayOrder { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
    public MenuCategory MenuCategory { get; private set; } = null!;

    public void UpdateCatalog(string name, string? description, string? accentLabel, string? imageUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        AccentLabel = string.IsNullOrWhiteSpace(accentLabel) ? null : accentLabel.Trim();
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        Touch();
    }

    public void ChangeCategory(Guid menuCategoryId)
    {
        if (menuCategoryId == Guid.Empty)
        {
            throw new ArgumentException("Category must be informed.", nameof(menuCategoryId));
        }

        MenuCategoryId = menuCategoryId;
        Touch();
    }

    public void UpdatePrice(decimal price)
    {
        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
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
