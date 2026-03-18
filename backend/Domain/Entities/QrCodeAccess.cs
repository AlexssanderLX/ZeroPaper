using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public class QrCodeAccess : TenantOwnedEntity
{
    private QrCodeAccess()
    {
    }

    public QrCodeAccess(
        Guid tenantId,
        Guid companyId,
        string label,
        string accessPath,
        bool isSingleUse = false,
        DateTime? expiresAtUtc = null,
        string? publicCode = null) : base(tenantId)
    {
        CompanyId = companyId;
        PublicCode = string.IsNullOrWhiteSpace(publicCode) ? Guid.NewGuid().ToString("N") : publicCode.Trim().ToLowerInvariant();
        UpdateDestination(label, accessPath);
        IsSingleUse = isSingleUse;
        ExpiresAtUtc = expiresAtUtc;
        Status = QrCodeStatus.Active;
    }

    public Guid CompanyId { get; private set; }
    public string Label { get; private set; } = null!;
    public string PublicCode { get; private set; } = null!;
    public string AccessPath { get; private set; } = null!;
    public bool IsSingleUse { get; private set; }
    public int ScanCount { get; private set; }
    public DateTime? LastScanAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public QrCodeStatus Status { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
    public DiningTable? DiningTable { get; private set; }

    public void UpdateDestination(string label, string accessPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessPath);

        Label = label.Trim();
        AccessPath = accessPath.Trim();
        Touch();
    }

    public void SetExpiration(DateTime? expiresAtUtc)
    {
        ExpiresAtUtc = expiresAtUtc;
        Touch();
    }

    public void Disable()
    {
        Status = QrCodeStatus.Disabled;
        Touch();
    }

    public void RegisterScan()
    {
        if (Status != QrCodeStatus.Active)
        {
            throw new InvalidOperationException("Only active QR codes can be used.");
        }

        if (ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            Status = QrCodeStatus.Expired;
            Touch();
            throw new InvalidOperationException("This QR code has expired.");
        }

        ScanCount++;
        LastScanAtUtc = DateTime.UtcNow;

        if (IsSingleUse)
        {
            Status = QrCodeStatus.Disabled;
        }

        Touch();
    }
}
