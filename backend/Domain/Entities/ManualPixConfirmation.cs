using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public class ManualPixConfirmation : TenantOwnedEntity
{
    private ManualPixConfirmation()
    {
    }

    public ManualPixConfirmation(
        Guid tenantId,
        Guid companyId,
        Guid orderId,
        string? customerName,
        string customerPhone,
        decimal amount,
        string pixKeyShown,
        string confirmationPhrase) : base(tenantId)
    {
        CompanyId = companyId;
        OrderId = orderId;
        CustomerName = NormalizeOptionalValue(customerName, 120);
        CustomerPhone = NormalizeRequiredValue(customerPhone, 40, nameof(customerPhone));
        Amount = decimal.Round(amount, 2);
        PixKeyShown = NormalizeRequiredValue(pixKeyShown, 180, nameof(pixKeyShown));
        ConfirmationPhrase = NormalizeRequiredValue(confirmationPhrase, 180, nameof(confirmationPhrase));
        Status = ManualPixConfirmationStatus.Pending;
    }

    public Guid CompanyId { get; private set; }
    public Guid OrderId { get; private set; }
    public string? CustomerName { get; private set; }
    public string CustomerPhone { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string PixKeyShown { get; private set; } = null!;
    public string ConfirmationPhrase { get; private set; } = null!;
    public string? CustomerMessage { get; private set; }
    public string? ReceiptReference { get; private set; }
    public ManualPixConfirmationStatus Status { get; private set; }
    public DateTime? CustomerConfirmedAtUtc { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public string? OwnerNote { get; private set; }

    public Company Company { get; private set; } = null!;
    public CustomerOrder Order { get; private set; } = null!;
    public AppUser? ReviewedByUser { get; private set; }

    public bool MatchesConfirmationPhrase(string? message)
    {
        return !string.IsNullOrWhiteSpace(message) &&
               string.Equals(message.Trim(), ConfirmationPhrase, StringComparison.OrdinalIgnoreCase);
    }

    public void AttachReceiptReference(string? receiptReference)
    {
        var normalized = NormalizeOptionalValue(receiptReference, 500);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        ReceiptReference = normalized;
        Touch();
    }

    public void RegisterCustomerConfirmation(string customerMessage, string? receiptReference = null)
    {
        CustomerMessage = NormalizeRequiredValue(customerMessage, 1000, nameof(customerMessage));
        CustomerConfirmedAtUtc ??= DateTime.UtcNow;
        AttachReceiptReference(receiptReference);
        Touch();
    }

    public void Review(ManualPixConfirmationStatus status, Guid reviewedByUserId, string? ownerNote)
    {
        Status = status;
        OwnerNote = NormalizeOptionalValue(ownerNote, 1000);

        if (status == ManualPixConfirmationStatus.Pending)
        {
            ReviewedAtUtc = null;
            ReviewedByUserId = null;
        }
        else
        {
            ReviewedAtUtc = DateTime.UtcNow;
            ReviewedByUserId = reviewedByUserId;
        }

        Touch();
    }

    private static string NormalizeRequiredValue(string value, int maxLength, string fieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"O campo {fieldName} precisa ter no maximo {maxLength} caracteres.", fieldName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalValue(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
