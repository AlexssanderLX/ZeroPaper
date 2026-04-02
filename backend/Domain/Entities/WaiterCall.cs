using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class WaiterCall : TenantOwnedEntity
{
    private WaiterCall()
    {
    }

    public WaiterCall(
        Guid tenantId,
        Guid companyId,
        Guid diningTableId) : base(tenantId)
    {
        CompanyId = companyId;
        DiningTableId = diningTableId;
        RequestedAtUtc = DateTime.UtcNow;
    }

    public Guid CompanyId { get; private set; }
    public Guid DiningTableId { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
    public DiningTable DiningTable { get; private set; } = null!;

    public void Repeat()
    {
        RequestedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Resolve()
    {
        ResolvedAtUtc = DateTime.UtcNow;
        Touch();
    }
}
