using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class CustomerOrderHistory : TenantOwnedEntity
{
    private readonly List<CustomerOrderHistoryItem> _items = [];

    private CustomerOrderHistory()
    {
    }

    public CustomerOrderHistory(
        Guid tenantId,
        Guid companyId,
        Guid customerProfileId,
        Guid orderId,
        decimal totalAmount,
        DateTime createdAtUtc,
        IEnumerable<CustomerOrderHistoryItem> items) : base(tenantId)
    {
        CompanyId = companyId;
        CustomerProfileId = customerProfileId;
        OrderId = orderId;
        TotalAmount = decimal.Round(totalAmount, 2);
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;

        foreach (var item in items)
        {
            _items.Add(item);
        }
    }

    public Guid CompanyId { get; private set; }
    public Guid CustomerProfileId { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal TotalAmount { get; private set; }

    public DeliveryCustomerProfile CustomerProfile { get; private set; } = null!;
    public IReadOnlyCollection<CustomerOrderHistoryItem> Items => _items.AsReadOnly();
}
