using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class DemoAccessLink : TenantOwnedEntity
{
    private DemoAccessLink() { }

    public DemoAccessLink(Guid tenantId, Guid companyId, Guid appUserId, string tokenHash, Guid createdByUserId)
        : base(tenantId)
    {
        CompanyId = companyId;
        AppUserId = appUserId;
        CreatedByUserId = createdByUserId;
        TokenHash = string.IsNullOrWhiteSpace(tokenHash)
            ? throw new ArgumentException("O hash do link demo e obrigatorio.", nameof(tokenHash))
            : tokenHash.Trim();
    }

    public Guid CompanyId { get; private set; }
    public Guid AppUserId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime? LastUsedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    public Company Company { get; private set; } = null!;
    public AppUser AppUser { get; private set; } = null!;
    public AppUser CreatedByUser { get; private set; } = null!;

    public bool IsAvailable() => IsActive && RevokedAtUtc is null;

    public void RegisterUsage(DateTime usedAtUtc)
    {
        LastUsedAtUtc = usedAtUtc;
        Touch();
    }

    public void Revoke(DateTime revokedAtUtc)
    {
        RevokedAtUtc = revokedAtUtc;
        Deactivate();
    }
}
