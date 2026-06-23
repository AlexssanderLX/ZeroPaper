using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class OrderItem : TenantOwnedEntity
{
    private readonly List<OrderItemAdditionalSelection> _additionalSelections = [];

    private OrderItem()
    {
    }

    public OrderItem(
        Guid tenantId,
        string name,
        decimal quantity,
        decimal unitPrice,
        Guid? sourceMenuItemId = null,
        string? categoryName = null,
        string? imageUrl = null,
        string? notes = null,
        decimal? baseUnitPrice = null,
        IEnumerable<OrderItemAdditionalSelection>? additionalSelections = null) : base(tenantId)
    {
        SourceMenuItemId = sourceMenuItemId;
        Rename(name);
        UpdateQuantity(quantity);
        UpdateBaseUnitPrice(baseUnitPrice ?? unitPrice);
        UpdateCategory(categoryName);
        UpdateImage(imageUrl);
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        if (additionalSelections is not null)
        {
            ReplaceAdditionalSelections(additionalSelections);
        }
    }

    public Guid CustomerOrderId { get; private set; }
    public Guid? SourceMenuItemId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? CategoryName { get; private set; }
    public string? ImageUrl { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal BaseUnitPrice { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string? Notes { get; private set; }
    public decimal TotalPrice => Quantity * UnitPrice;

    public CustomerOrder CustomerOrder { get; private set; } = null!;
    public IReadOnlyCollection<OrderItemAdditionalSelection> AdditionalSelections => _additionalSelections.AsReadOnly();

    public void AttachToCustomerOrder(Guid customerOrderId)
    {
        if (customerOrderId == Guid.Empty)
        {
            throw new ArgumentException("Customer order id must be informed.", nameof(customerOrderId));
        }

        CustomerOrderId = customerOrderId;
        Touch();
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Touch();
    }

    public void UpdateCategory(string? categoryName)
    {
        CategoryName = string.IsNullOrWhiteSpace(categoryName) ? null : categoryName.Trim();
        Touch();
    }

    public void UpdateImage(string? imageUrl)
    {
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
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

    public void UpdateBaseUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0)
        {
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        }

        BaseUnitPrice = unitPrice;
        RecalculateUnitPrice();
        Touch();
    }

    public void ReplaceAdditionalSelections(IEnumerable<OrderItemAdditionalSelection> additionalSelections)
    {
        ArgumentNullException.ThrowIfNull(additionalSelections);

        _additionalSelections.Clear();
        _additionalSelections.AddRange(additionalSelections);
        RecalculateUnitPrice();
        Touch();
    }

    private void RecalculateUnitPrice()
    {
        UnitPrice = BaseUnitPrice + _additionalSelections.Sum(item => item.UnitPrice);
    }
}
