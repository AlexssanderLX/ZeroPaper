using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class DeliveryDistanceCache : TenantOwnedEntity
{
    private DeliveryDistanceCache()
    {
    }

    public DeliveryDistanceCache(
        Guid tenantId,
        Guid companyId,
        string originPostalCode,
        string destinationPostalCode,
        string provider,
        decimal distanceKm,
        DateTime expiresAtUtc) : base(tenantId)
    {
        CompanyId = companyId;
        OriginPostalCode = NormalizePostalCode(originPostalCode, nameof(originPostalCode));
        DestinationPostalCode = NormalizePostalCode(destinationPostalCode, nameof(destinationPostalCode));
        Provider = NormalizeProvider(provider);
        UpdateDistance(distanceKm, expiresAtUtc);
    }

    public Guid CompanyId { get; private set; }
    public string OriginPostalCode { get; private set; } = null!;
    public string DestinationPostalCode { get; private set; } = null!;
    public string Provider { get; private set; } = null!;
    public decimal DistanceKm { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }

    public Company Company { get; private set; } = null!;

    public void UpdateDistance(decimal distanceKm, DateTime expiresAtUtc)
    {
        if (distanceKm <= 0)
        {
            throw new ArgumentException("A distancia precisa ser maior que zero.", nameof(distanceKm));
        }

        DistanceKm = decimal.Round(distanceKm, 2);
        ExpiresAtUtc = expiresAtUtc;
        Touch();
    }

    private static string NormalizePostalCode(string value, string fieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
        {
            throw new ArgumentException("Use um CEP valido com 8 digitos.", fieldName);
        }

        return digits;
    }

    private static string NormalizeProvider(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim().ToLowerInvariant();
    }
}
