using System.Security.Cryptography;
using System.Text;
using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class SignupCode : BaseEntity
{
    private SignupCode()
    {
    }

    public SignupCode(
        string label,
        string rawCode,
        DateTime expiresAtUtc,
        int maxUses,
        Guid? createdByUserId = null,
        string? boundEmail = null,
        string? allowedPlanName = null,
        int? allowedMaxUsers = null)
    {
        UpdateLabel(label);
        ReplaceCodeHash(rawCode);
        UpdateBoundEmail(boundEmail);
        SetLifetime(expiresAtUtc);
        SetMaxUses(maxUses);
        AllowedPlanName = string.IsNullOrWhiteSpace(allowedPlanName) ? null : allowedPlanName.Trim();
        AllowedMaxUsers = allowedMaxUsers;
        CreatedByUserId = createdByUserId;
    }

    public string Label { get; private set; } = null!;
    public string CodeHash { get; private set; } = null!;
    public string? BoundEmail { get; private set; }
    public string? AllowedPlanName { get; private set; }
    public int? AllowedMaxUsers { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public int MaxUses { get; private set; }
    public int UsedCount { get; private set; }
    public DateTime? LastUsedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    public bool IsAvailable(string? email = null, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var normalizedEmail = NormalizeOptionalEmail(email);

        return IsActive &&
               ExpiresAtUtc > now &&
               UsedCount < MaxUses &&
               (BoundEmail is null || BoundEmail == normalizedEmail);
    }

    public bool Matches(string rawCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawCode);
        return CodeHash == ComputeCodeHash(rawCode);
    }

    public void RegisterUse(DateTime utcNow)
    {
        if (!IsAvailable(utcNow: utcNow))
        {
            throw new InvalidOperationException("Signup code is no longer available.");
        }

        UsedCount++;
        LastUsedAtUtc = utcNow;

        if (UsedCount >= MaxUses)
        {
            Deactivate();
            return;
        }

        Touch();
    }

    public static string GenerateRawCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(9);
        var raw = Convert.ToHexString(bytes);
        return $"ZP-{raw[0..4]}-{raw[4..8]}-{raw[8..12]}-{raw[12..18]}";
    }

    public static string NormalizeRawCode(string rawCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawCode);
        return rawCode.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }

    private void UpdateLabel(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        Label = label.Trim();
        Touch();
    }

    private void ReplaceCodeHash(string rawCode)
    {
        CodeHash = ComputeCodeHash(rawCode);
        Touch();
    }

    private void UpdateBoundEmail(string? email)
    {
        BoundEmail = NormalizeOptionalEmail(email);
        Touch();
    }

    private void SetLifetime(DateTime expiresAtUtc)
    {
        if (expiresAtUtc <= DateTime.UtcNow)
        {
            throw new ArgumentException("Signup code expiration must be in the future.", nameof(expiresAtUtc));
        }

        ExpiresAtUtc = expiresAtUtc;
        Touch();
    }

    private void SetMaxUses(int maxUses)
    {
        if (maxUses <= 0)
        {
            throw new ArgumentException("Signup code max uses must be greater than zero.", nameof(maxUses));
        }

        MaxUses = maxUses;
        Touch();
    }

    private static string ComputeCodeHash(string rawCode)
    {
        var normalizedCode = NormalizeRawCode(rawCode);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedCode));
        return Convert.ToHexString(hashBytes);
    }

    private static string? NormalizeOptionalEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }
}
