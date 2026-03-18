using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class StockItem : TenantOwnedEntity
{
    private StockItem()
    {
    }

    public StockItem(
        Guid tenantId,
        Guid companyId,
        string name,
        string category,
        string unit,
        decimal currentQuantity,
        decimal minimumQuantity) : base(tenantId)
    {
        CompanyId = companyId;
        UpdateCatalog(name, category, unit);
        UpdateStockLevels(currentQuantity, minimumQuantity);
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public string Unit { get; private set; } = null!;
    public decimal CurrentQuantity { get; private set; }
    public decimal MinimumQuantity { get; private set; }
    public DateTime? LastRestockedAtUtc { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;

    public void UpdateCatalog(string name, string category, string unit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);

        Name = name.Trim();
        Category = category.Trim();
        Unit = unit.Trim();
        Touch();
    }

    public void UpdateStockLevels(decimal currentQuantity, decimal minimumQuantity)
    {
        if (currentQuantity < 0)
        {
            throw new ArgumentException("Current quantity cannot be negative.", nameof(currentQuantity));
        }

        if (minimumQuantity < 0)
        {
            throw new ArgumentException("Minimum quantity cannot be negative.", nameof(minimumQuantity));
        }

        CurrentQuantity = currentQuantity;
        MinimumQuantity = minimumQuantity;

        if (currentQuantity > 0)
        {
            LastRestockedAtUtc = DateTime.UtcNow;
        }

        Touch();
    }
}

