using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class DeliveryCustomerProfile : TenantOwnedEntity
{
    private DeliveryCustomerProfile()
    {
    }

    public DeliveryCustomerProfile(
        Guid tenantId,
        Guid companyId,
        string phone,
        string? customerName,
        string? deliveryAddress,
        string? deliveryNumber,
        string? deliveryNeighborhood,
        string? deliveryComplement,
        string? deliveryPostalCode,
        DateTime lastOrderAtUtc) : base(tenantId)
    {
        CompanyId = companyId;
        Phone = NormalizePhone(phone);
        Update(customerName, deliveryAddress, deliveryNumber, deliveryNeighborhood, deliveryComplement, deliveryPostalCode, lastOrderAtUtc);
    }

    public Guid CompanyId { get; private set; }
    public string Phone { get; private set; } = null!;
    public string? CustomerName { get; private set; }
    public string? DeliveryAddress { get; private set; }
    public string? DeliveryNumber { get; private set; }
    public string? DeliveryNeighborhood { get; private set; }
    public string? DeliveryComplement { get; private set; }
    public string? DeliveryPostalCode { get; private set; }
    public DateTime LastOrderAtUtc { get; private set; }
    public string? PublicAccessCodeHash { get; private set; }
    public string? PublicAccessCodeCipherText { get; private set; }
    public DateTime? PublicAccessCodeCreatedAtUtc { get; private set; }

    public Company Company { get; private set; } = null!;

    public void Update(
        string? customerName,
        string? deliveryAddress,
        string? deliveryNumber,
        string? deliveryNeighborhood,
        string? deliveryComplement,
        string? deliveryPostalCode,
        DateTime lastOrderAtUtc)
    {
        CustomerName = NormalizeOptional(customerName, 120);
        DeliveryAddress = NormalizeOptional(deliveryAddress, 220);
        DeliveryNumber = NormalizeOptional(deliveryNumber, 30);
        DeliveryNeighborhood = NormalizeOptional(deliveryNeighborhood, 120);
        DeliveryComplement = NormalizeOptional(deliveryComplement, 160);
        DeliveryPostalCode = NormalizePostalCodeOrNull(deliveryPostalCode);
        LastOrderAtUtc = lastOrderAtUtc;
        Touch();
    }

    public void UpdateOwnerData(
        string? customerName,
        string? deliveryAddress,
        string? deliveryNumber,
        string? deliveryNeighborhood,
        string? deliveryComplement,
        string? deliveryPostalCode)
    {
        CustomerName = NormalizeOptional(customerName, 120);
        DeliveryAddress = NormalizeOptional(deliveryAddress, 220);
        DeliveryNumber = NormalizeOptional(deliveryNumber, 30);
        DeliveryNeighborhood = NormalizeOptional(deliveryNeighborhood, 120);
        DeliveryComplement = NormalizeOptional(deliveryComplement, 160);
        DeliveryPostalCode = NormalizePostalCodeOrNull(deliveryPostalCode);
        Touch();
    }

    public void SetPublicAccessCode(string codeHash, string codeCipherText, DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(codeCipherText);

        PublicAccessCodeHash = codeHash.Trim();
        PublicAccessCodeCipherText = codeCipherText.Trim();
        PublicAccessCodeCreatedAtUtc = createdAtUtc;
        Touch();
    }

    public static string NormalizePhone(string phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length is 10 or 11)
        {
            return $"55{digits}";
        }

        if (string.IsNullOrWhiteSpace(digits))
        {
            throw new ArgumentException("Telefone invalido.", nameof(phone));
        }

        return digits;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? NormalizePostalCodeOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 8 ? digits : null;
    }
}
