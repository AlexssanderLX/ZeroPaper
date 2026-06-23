using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class OrderItemAdditionalSelection : TenantOwnedEntity
{
    private OrderItemAdditionalSelection()
    {
    }

    public OrderItemAdditionalSelection(
        Guid tenantId,
        string groupName,
        string optionName,
        decimal unitPrice,
        Guid? sourceMenuItemAdditionalOptionId = null) : base(tenantId)
    {
        SourceMenuItemAdditionalOptionId = sourceMenuItemAdditionalOptionId;
        Rename(groupName, optionName);
        UpdateUnitPrice(unitPrice);
    }

    public Guid OrderItemId { get; private set; }
    public Guid? SourceMenuItemAdditionalOptionId { get; private set; }
    public string GroupName { get; private set; } = null!;
    public string OptionName { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }

    public OrderItem OrderItem { get; private set; } = null!;

    public void AttachToOrderItem(Guid orderItemId)
    {
        if (orderItemId == Guid.Empty)
        {
            throw new ArgumentException("Order item id must be informed.", nameof(orderItemId));
        }

        OrderItemId = orderItemId;
        Touch();
    }

    public void Rename(string groupName, string optionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        ArgumentException.ThrowIfNullOrWhiteSpace(optionName);
        GroupName = groupName.Trim();
        OptionName = optionName.Trim();
        Touch();
    }

    public void UpdateUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Additional price cannot be negative.");
        }

        UnitPrice = unitPrice;
        Touch();
    }
}
