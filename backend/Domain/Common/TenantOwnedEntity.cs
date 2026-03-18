namespace ZeroPaper.Domain.Common;

public abstract class TenantOwnedEntity : BaseEntity
{
    protected TenantOwnedEntity()
    {
    }

    protected TenantOwnedEntity(Guid tenantId)
    {
        TenantId = tenantId;
    }

    public Guid TenantId { get; protected set; }
}
