using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public class DiningTable : TenantOwnedEntity
{
    private readonly List<CustomerOrder> _orders = [];

    private DiningTable()
    {
    }

    public DiningTable(
        Guid tenantId,
        Guid companyId,
        Guid qrCodeAccessId,
        string name,
        string internalCode,
        int seats) : base(tenantId)
    {
        CompanyId = companyId;
        QrCodeAccessId = qrCodeAccessId;
        Rename(name);
        ChangeInternalCode(internalCode);
        UpdateSeats(seats);
        Status = TableStatus.Available;
    }

    public Guid CompanyId { get; private set; }
    public Guid QrCodeAccessId { get; private set; }
    public string Name { get; private set; } = null!;
    public string InternalCode { get; private set; } = null!;
    public int Seats { get; private set; }
    public TableStatus Status { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
    public QrCodeAccess QrCodeAccess { get; private set; } = null!;
    public IReadOnlyCollection<CustomerOrder> Orders => _orders.AsReadOnly();

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Touch();
    }

    public void ChangeInternalCode(string internalCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(internalCode);
        InternalCode = internalCode.Trim().ToUpperInvariant();
        Touch();
    }

    public void UpdateSeats(int seats)
    {
        if (seats <= 0)
        {
            throw new ArgumentException("Seats must be greater than zero.", nameof(seats));
        }

        Seats = seats;
        Touch();
    }

    public void ChangeStatus(TableStatus status)
    {
        Status = status;
        Touch();
    }
}

