using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class CustomerOrderHistoryItem : TenantOwnedEntity
{
    private CustomerOrderHistoryItem()
    {
    }

    public CustomerOrderHistoryItem(Guid tenantId, string itemName, decimal quantity) : base(tenantId)
    {
        ItemName = NormalizeName(itemName);
        Quantity = quantity > 0 ? quantity : throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
    }

    public Guid CustomerOrderHistoryId { get; private set; }
    public string ItemName { get; private set; } = null!;
    public decimal Quantity { get; private set; }

    public CustomerOrderHistory CustomerOrderHistory { get; private set; } = null!;

    private static string NormalizeName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        return normalized.Length <= 160 ? normalized : normalized[..160];
    }
}
