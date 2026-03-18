using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class OrderItem : TenantOwnedEntity
{
    private OrderItem()
    {
    }

    public OrderItem(
        Guid tenantId,
        string name,
        decimal quantity,
        decimal unitPrice,
        string? notes = null) : base(tenantId)
    {
        Rename(name);
        UpdateQuantity(quantity);
        UpdateUnitPrice(unitPrice);
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public Guid CustomerOrderId { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string? Notes { get; private set; }
    public decimal TotalPrice => Quantity * UnitPrice;

    public CustomerOrder CustomerOrder { get; private set; } = null!;

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Touch();
    }

    public void UpdateQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        Quantity = quantity;
        Touch();
    }

    public void UpdateUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0)
        {
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        }

        UnitPrice = unitPrice;
        Touch();
    }
}

