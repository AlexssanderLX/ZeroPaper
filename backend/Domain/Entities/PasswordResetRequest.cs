using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class PasswordResetRequest : BaseEntity
{
    private PasswordResetRequest()
    {
    }

    public PasswordResetRequest(Guid appUserId, string tokenHash, DateTime expiresAtUtc)
    {
        AppUserId = appUserId;
        ReplaceTokenHash(tokenHash);
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid AppUserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }

    public AppUser AppUser { get; private set; } = null!;

    public bool IsAvailable(DateTime utcNow)
    {
        return IsActive && UsedAtUtc is null && ExpiresAtUtc > utcNow;
    }

    public void MarkAsUsed(DateTime utcNow)
    {
        UsedAtUtc = utcNow;
        Deactivate();
    }

    public void Revoke()
    {
        Deactivate();
    }

    private void ReplaceTokenHash(string tokenHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        TokenHash = tokenHash.Trim();
        Touch();
    }
}
