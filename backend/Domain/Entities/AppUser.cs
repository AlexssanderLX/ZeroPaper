using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public class AppUser : TenantOwnedEntity
{
    private readonly List<AppSession> _sessions = [];

    private AppUser()
    {
    }

    public AppUser(
        Guid tenantId,
        Guid companyId,
        string fullName,
        string email,
        string passwordHash,
        UserRole role) : base(tenantId)
    {
        CompanyId = companyId;
        ChangeIdentity(fullName, email);
        ChangePasswordHash(passwordHash);
        ChangeRole(role);
    }

    public Guid CompanyId { get; private set; }
    public string FullName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }
    public string? ShortcutAccessTokenHash { get; private set; }
    public DateTime? ShortcutAccessCreatedAtUtc { get; private set; }
    public DateTime? ShortcutAccessExpiresAtUtc { get; private set; }
    public DateTime? ShortcutAccessLastUsedAtUtc { get; private set; }
    public DateTime? ShortcutAccessRevokedAtUtc { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
    public IReadOnlyCollection<AppSession> Sessions => _sessions.AsReadOnly();

    public void ChangeIdentity(string fullName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Touch();
    }

    public void ChangePasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash.Trim();
        Touch();
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
        Touch();
    }

    public void RegisterLogin()
    {
        LastLoginAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void RotateShortcutAccessToken(string tokenHash, DateTime createdAtUtc, DateTime expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException("A validade do atalho precisa ser futura.", nameof(expiresAtUtc));
        }

        ShortcutAccessTokenHash = tokenHash.Trim();
        ShortcutAccessCreatedAtUtc = createdAtUtc;
        ShortcutAccessExpiresAtUtc = expiresAtUtc;
        ShortcutAccessLastUsedAtUtc = null;
        ShortcutAccessRevokedAtUtc = null;
        Touch();
    }

    public void RegisterShortcutAccessUsage(DateTime usedAtUtc)
    {
        ShortcutAccessLastUsedAtUtc = usedAtUtc;
        Touch();
    }

    public void RevokeShortcutAccess(DateTime revokedAtUtc)
    {
        ShortcutAccessTokenHash = null;
        ShortcutAccessCreatedAtUtc = null;
        ShortcutAccessExpiresAtUtc = null;
        ShortcutAccessLastUsedAtUtc = null;
        ShortcutAccessRevokedAtUtc = revokedAtUtc;
        Touch();
    }

    public bool HasActiveShortcutAccess(DateTime utcNow)
    {
        return !string.IsNullOrWhiteSpace(ShortcutAccessTokenHash) &&
               ShortcutAccessRevokedAtUtc is null &&
               ShortcutAccessExpiresAtUtc.HasValue &&
               ShortcutAccessExpiresAtUtc.Value > utcNow;
    }
}
