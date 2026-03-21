using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public class CustomerOrder : TenantOwnedEntity
{
    private readonly List<OrderItem> _items = [];

    private CustomerOrder()
    {
    }

    public CustomerOrder(
        Guid tenantId,
        Guid companyId,
        Guid diningTableId,
        int number,
        string? customerName,
        string? notes,
        IEnumerable<OrderItem> items,
        PaymentMethod paymentMethod) : base(tenantId)
    {
        CompanyId = companyId;
        DiningTableId = diningTableId;
        Number = number;
        CustomerName = string.IsNullOrWhiteSpace(customerName) ? null : customerName.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        SubmittedAtUtc = DateTime.UtcNow;
        Status = OrderStatus.Pending;
        PaymentMethod = paymentMethod;
        PaymentStatus = PaymentStatus.Pending;

        foreach (var item in items)
        {
            _items.Add(item);
        }

        RecalculateTotal();
    }

    public Guid CompanyId { get; private set; }
    public Guid DiningTableId { get; private set; }
    public int Number { get; private set; }
    public string? CustomerName { get; private set; }
    public string? Notes { get; private set; }
    public OrderStatus Status { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; }
    public DateTime? SentToKitchenAtUtc { get; private set; }
    public DateTime? ReadyAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
    public DiningTable DiningTable { get; private set; } = null!;
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public void MoveToKitchen()
    {
        if (Status == OrderStatus.Cancelled || Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException("Closed orders cannot move to kitchen.");
        }

        Status = OrderStatus.InKitchen;
        SentToKitchenAtUtc ??= DateTime.UtcNow;
        Touch();
    }

    public void MarkReady()
    {
        if (Status == OrderStatus.Cancelled || Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException("Closed orders cannot be updated.");
        }

        Status = OrderStatus.Ready;
        ReadyAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void MarkDelivered()
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled orders cannot be delivered.");
        }

        Status = OrderStatus.Delivered;
        ClosedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Delivered)
        {
            throw new InvalidOperationException("Delivered orders cannot be cancelled.");
        }

        Status = OrderStatus.Cancelled;
        ClosedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void MarkPaid()
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled orders cannot be marked as paid.");
        }

        PaymentStatus = PaymentStatus.Paid;
        PaidAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void MarkPaymentPending()
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled orders cannot change payment.");
        }

        PaymentStatus = PaymentStatus.Pending;
        PaidAtUtc = null;
        Touch();
    }

    public void UpdatePaymentMethod(PaymentMethod paymentMethod)
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled orders cannot change payment.");
        }

        PaymentMethod = paymentMethod;
        Touch();
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(item => item.TotalPrice);
        Touch();
    }
}
