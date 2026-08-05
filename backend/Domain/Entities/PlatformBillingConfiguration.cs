using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public sealed class PlatformBillingConfiguration : BaseEntity
{
    private PlatformBillingConfiguration() { }

    public PlatformBillingConfiguration(
        string accessTokenCipherText,
        string accountUserId,
        string? accountEmail,
        bool liveMode,
        Guid updatedByUserId)
    {
        Update(accessTokenCipherText, accountUserId, accountEmail, liveMode, updatedByUserId);
    }

    public string Provider { get; private set; } = "MercadoPago";
    public string AccessTokenCipherText { get; private set; } = null!;
    public string AccountUserId { get; private set; } = null!;
    public string? AccountEmail { get; private set; }
    public bool LiveMode { get; private set; }
    public Guid UpdatedByUserId { get; private set; }

    public void Update(string accessTokenCipherText, string accountUserId, string? accountEmail, bool liveMode, Guid updatedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessTokenCipherText);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountUserId);
        AccessTokenCipherText = accessTokenCipherText;
        AccountUserId = accountUserId.Trim();
        AccountEmail = string.IsNullOrWhiteSpace(accountEmail) ? null : accountEmail.Trim().ToLowerInvariant();
        LiveMode = liveMode;
        UpdatedByUserId = updatedByUserId;
        Activate();
    }
}
