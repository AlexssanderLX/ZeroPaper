using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class AppSession : TenantOwnedEntity
{
    private AppSession()
    {
    }

    public AppSession(
        Guid tenantId,
        Guid companyId,
        Guid appUserId,
        string tokenHash,
        DateTime expiresAtUtc) : base(tenantId)
    {
        CompanyId = companyId;
        AppUserId = appUserId;
        ReplaceTokenHash(tokenHash);
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid CompanyId { get; private set; }
    public Guid AppUserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? LastSeenAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
    public AppUser AppUser { get; private set; } = null!;

    public bool IsAvailable(DateTime utcNow)
    {
        return IsActive && RevokedAtUtc is null && ExpiresAtUtc > utcNow;
    }

    public void RegisterUsage(DateTime utcNow)
    {
        LastSeenAtUtc = utcNow;
        Touch();
    }

    public void Revoke(DateTime utcNow)
    {
        RevokedAtUtc = utcNow;
        Deactivate();
    }

    private void ReplaceTokenHash(string tokenHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        TokenHash = tokenHash.Trim();
        Touch();
    }
}

