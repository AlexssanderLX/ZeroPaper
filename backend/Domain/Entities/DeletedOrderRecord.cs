using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public class DeletedOrderRecord : TenantOwnedEntity
{
    private DeletedOrderRecord()
    {
    }

    public DeletedOrderRecord(
        Guid tenantId,
        Guid companyId,
        Guid sourceOrderId,
        int orderNumber,
        string tableName,
        string? customerName,
        string? notes,
        string itemsSummary,
        OrderStatus status,
        PaymentMethod paymentMethod,
        PaymentMethod requestedPaymentMethod,
        PaymentStatus paymentStatus,
        PrintStatus printStatus,
        decimal totalAmount,
        DateTime submittedAtUtc,
        DateTime? paidAtUtc,
        DateTime? printedAtUtc,
        DateTime deletedAtUtc,
        Guid deletedByUserId,
        string deletedByUserName,
        string deletionReason) : base(tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemsSummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(deletedByUserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(deletionReason);

        CompanyId = companyId;
        SourceOrderId = sourceOrderId;
        OrderNumber = orderNumber;
        TableName = tableName.Trim();
        CustomerName = string.IsNullOrWhiteSpace(customerName) ? null : customerName.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        ItemsSummary = itemsSummary.Trim();
        Status = status;
        PaymentMethod = paymentMethod;
        RequestedPaymentMethod = requestedPaymentMethod;
        PaymentStatus = paymentStatus;
        PrintStatus = printStatus;
        TotalAmount = totalAmount;
        SubmittedAtUtc = submittedAtUtc;
        PaidAtUtc = paidAtUtc;
        PrintedAtUtc = printedAtUtc;
        DeletedAtUtc = deletedAtUtc;
        DeletedByUserId = deletedByUserId;
        DeletedByUserName = deletedByUserName.Trim();
        DeletionReason = deletionReason.Trim();
    }

    public Guid CompanyId { get; private set; }
    public Guid SourceOrderId { get; private set; }
    public int OrderNumber { get; private set; }
    public string TableName { get; private set; } = null!;
    public string? CustomerName { get; private set; }
    public string? Notes { get; private set; }
    public string ItemsSummary { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentMethod RequestedPaymentMethod { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public PrintStatus PrintStatus { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime? PrintedAtUtc { get; private set; }
    public DateTime DeletedAtUtc { get; private set; }
    public Guid DeletedByUserId { get; private set; }
    public string DeletedByUserName { get; private set; } = null!;
    public string DeletionReason { get; private set; } = null!;

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
}
