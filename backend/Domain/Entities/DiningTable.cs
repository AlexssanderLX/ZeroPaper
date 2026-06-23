using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public class DiningTable : TenantOwnedEntity
{
    private readonly List<CustomerOrder> _orders = [];
    private readonly List<WaiterCall> _waiterCalls = [];

    private DiningTable()
    {
    }

    public DiningTable(
        Guid tenantId,
        Guid companyId,
        Guid qrCodeAccessId,
        string name,
        string internalCode,
        int seats,
        string? comandaLabel = null,
        bool isDeliveryChannel = false) : base(tenantId)
    {
        CompanyId = companyId;
        QrCodeAccessId = qrCodeAccessId;
        Rename(name);
        ChangeInternalCode(internalCode);
        UpdateSeats(seats);
        UpdateComandaLabel(comandaLabel);
        IsDeliveryChannel = isDeliveryChannel;
        Status = TableStatus.Available;
    }

    public Guid CompanyId { get; private set; }
    public Guid QrCodeAccessId { get; private set; }
    public string Name { get; private set; } = null!;
    public string InternalCode { get; private set; } = null!;
    public int Seats { get; private set; }
    public string? ComandaLabel { get; private set; }
    public TableStatus Status { get; private set; }
    public string? AlertSoundUrl { get; private set; }
    public bool IsDeliveryChannel { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
    public QrCodeAccess QrCodeAccess { get; private set; } = null!;
    public IReadOnlyCollection<CustomerOrder> Orders => _orders.AsReadOnly();
    public IReadOnlyCollection<WaiterCall> WaiterCalls => _waiterCalls.AsReadOnly();

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

    public void UpdateComandaLabel(string? comandaLabel)
    {
        if (string.IsNullOrWhiteSpace(comandaLabel))
        {
            ComandaLabel = null;
            Touch();
            return;
        }

        var normalizedValue = comandaLabel.Trim();

        if (normalizedValue.Length > 40)
        {
            throw new ArgumentException("Comanda label must be 40 characters or fewer.", nameof(comandaLabel));
        }

        ComandaLabel = normalizedValue;
        Touch();
    }

    public void ChangeStatus(TableStatus status)
    {
        Status = status;
        Touch();
    }

    public void UpdateAlertSound(string? alertSoundUrl)
    {
        AlertSoundUrl = string.IsNullOrWhiteSpace(alertSoundUrl) ? null : alertSoundUrl.Trim();
        Touch();
    }
}
