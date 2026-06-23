using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class DeliveryCustomerLinkService : IDeliveryCustomerLinkService
{
    private const string ProtectorPurpose = "ZeroPaper.Delivery.CustomerLink.v1";
    private const string ShortCodeProtectorPurpose = "ZeroPaper.Delivery.CustomerShortCode.v1";
    private static readonly TimeSpan TokenMaxAge = TimeSpan.FromDays(180);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ZeroPaperDbContext _context;
    private readonly IDataProtector _protector;
    private readonly IDataProtector _shortCodeProtector;

    public DeliveryCustomerLinkService(ZeroPaperDbContext context, IDataProtectionProvider dataProtectionProvider)
    {
        _context = context;
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _shortCodeProtector = dataProtectionProvider.CreateProtector(ShortCodeProtectorPurpose);
    }

    public string CreateToken(Guid companyId, string publicCode, string phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        var payload = new DeliveryCustomerLinkPayload
        {
            CompanyId = companyId,
            PublicCode = publicCode.Trim().ToLowerInvariant(),
            Phone = NormalizePhone(phone),
            IssuedAtUtc = DateTime.UtcNow
        };

        var protectedPayload = _protector.Protect(JsonSerializer.Serialize(payload, JsonOptions));
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(protectedPayload));
    }

    public bool TryReadToken(string? token, out DeliveryCustomerLinkPayload payload)
    {
        payload = new DeliveryCustomerLinkPayload();

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var protectedPayload = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token.Trim()));
            var json = _protector.Unprotect(protectedPayload);
            var decodedPayload = JsonSerializer.Deserialize<DeliveryCustomerLinkPayload>(json, JsonOptions);

            if (decodedPayload is null ||
                decodedPayload.CompanyId == Guid.Empty ||
                string.IsNullOrWhiteSpace(decodedPayload.PublicCode) ||
                string.IsNullOrWhiteSpace(decodedPayload.Phone) ||
                decodedPayload.IssuedAtUtc > DateTime.UtcNow.AddMinutes(5) ||
                decodedPayload.IssuedAtUtc < DateTime.UtcNow.Subtract(TokenMaxAge))
            {
                return false;
            }

            decodedPayload.PublicCode = decodedPayload.PublicCode.Trim().ToLowerInvariant();
            decodedPayload.Phone = NormalizePhone(decodedPayload.Phone);
            payload = decodedPayload;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetOrCreateShortCodeForCustomerAsync(
        Guid companyId,
        string phone,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty || string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var normalizedPhone = NormalizePhone(phone);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return null;
        }

        var profile = await _context.DeliveryCustomerProfiles
            .FirstOrDefaultAsync(
                item =>
                    item.CompanyId == companyId &&
                    item.Phone == normalizedPhone &&
                    item.IsActive,
                cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var existingCode = TryUnprotectShortCode(profile);
        if (!string.IsNullOrWhiteSpace(existingCode))
        {
            return existingCode;
        }

        for (var attempt = 0; attempt < 6; attempt++)
        {
            var code = CreateShortCode();
            var codeHash = ComputeCodeHash(code);
            var alreadyExists = await _context.DeliveryCustomerProfiles
                .AsNoTracking()
                .AnyAsync(item => item.PublicAccessCodeHash == codeHash, cancellationToken);

            if (alreadyExists)
            {
                continue;
            }

            profile.SetPublicAccessCode(codeHash, _shortCodeProtector.Protect(code), DateTime.UtcNow);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return code;
            }
            catch (DbUpdateException) when (attempt < 5)
            {
                // Extremely unlikely collision or race. Generate a fresh code and retry.
            }
        }

        throw new InvalidOperationException("Nao foi possivel gerar um link curto unico para o cliente.");
    }

    public async Task<DeliveryCustomerLinkPayload?> TryReadShortCodeAsync(
        string? code,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeShortCode(code);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return null;
        }

        var codeHash = ComputeCodeHash(normalizedCode);
        var profile = await _context.DeliveryCustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item =>
                    item.PublicAccessCodeHash == codeHash &&
                    item.IsActive,
                cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var storedCode = TryUnprotectShortCode(profile);
        if (!string.Equals(storedCode, normalizedCode, StringComparison.Ordinal))
        {
            return null;
        }

        return new DeliveryCustomerLinkPayload
        {
            CompanyId = profile.CompanyId,
            PublicCode = string.Empty,
            Phone = profile.Phone,
            IssuedAtUtc = profile.PublicAccessCodeCreatedAtUtc ?? profile.CreatedAtUtc
        };
    }

    private static string NormalizePhone(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length is 10 or 11)
        {
            return $"55{digits}";
        }

        return digits;
    }

    private string? TryUnprotectShortCode(DeliveryCustomerProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.PublicAccessCodeCipherText) ||
            string.IsNullOrWhiteSpace(profile.PublicAccessCodeHash))
        {
            return null;
        }

        try
        {
            var code = NormalizeShortCode(_shortCodeProtector.Unprotect(profile.PublicAccessCodeCipherText));
            if (string.IsNullOrWhiteSpace(code) ||
                !string.Equals(ComputeCodeHash(code), profile.PublicAccessCodeHash, StringComparison.Ordinal))
            {
                return null;
            }

            return code;
        }
        catch
        {
            return null;
        }
    }

    private static string CreateShortCode()
    {
        return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(16));
    }

    private static string ComputeCodeHash(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(NormalizeShortCode(code) ?? string.Empty));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? NormalizeShortCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code) ? null : code.Trim();
    }
}
