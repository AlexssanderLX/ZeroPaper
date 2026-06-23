using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class WhatsAppConversation : TenantOwnedEntity
{
    private readonly List<WhatsAppMessage> _messages = [];

    private WhatsAppConversation()
    {
    }

    public WhatsAppConversation(
        Guid tenantId,
        Guid companyId,
        string externalPhone,
        string? customerName = null) : base(tenantId)
    {
        CompanyId = companyId;
        ExternalPhone = NormalizePhone(externalPhone);
        UpdateCustomerName(customerName);
    }

    public Guid CompanyId { get; private set; }
    public string ExternalPhone { get; private set; } = null!;
    public string? CustomerName { get; private set; }
    public string? LastMessagePreview { get; private set; }
    public string LastDirection { get; private set; } = "Inbound";
    public DateTime? LastIncomingAtUtc { get; private set; }
    public DateTime? LastOutgoingAtUtc { get; private set; }
    public DateTime? LastInteractionAtUtc { get; private set; }

    public Company Company { get; private set; } = null!;
    public IReadOnlyCollection<WhatsAppMessage> Messages => _messages.AsReadOnly();

    public void RegisterInbound(string? customerName, string preview, DateTime occurredAtUtc)
    {
        UpdateCustomerName(customerName);
        LastMessagePreview = TrimPreview(preview);
        LastDirection = "Inbound";
        LastIncomingAtUtc = occurredAtUtc;
        LastInteractionAtUtc = occurredAtUtc;
        Touch();
    }

    public void RegisterOutbound(string preview, DateTime occurredAtUtc)
    {
        LastMessagePreview = TrimPreview(preview);
        LastDirection = "Outbound";
        LastOutgoingAtUtc = occurredAtUtc;
        LastInteractionAtUtc = occurredAtUtc;
        Touch();
    }

    private void UpdateCustomerName(string? customerName)
    {
        CustomerName = string.IsNullOrWhiteSpace(customerName)
            ? CustomerName
            : customerName.Trim();
    }

    private static string NormalizePhone(string phone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (string.IsNullOrWhiteSpace(digits))
        {
            throw new ArgumentException("Telefone invalido.", nameof(phone));
        }

        return digits;
    }

    private static string TrimPreview(string? preview)
    {
        if (string.IsNullOrWhiteSpace(preview))
        {
            return string.Empty;
        }

        var normalized = preview.Trim();
        return normalized.Length <= 280
            ? normalized
            : $"{normalized[..277]}...";
    }
}
