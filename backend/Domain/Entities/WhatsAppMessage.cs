using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class WhatsAppMessage : TenantOwnedEntity
{
    private WhatsAppMessage()
    {
    }

    public WhatsAppMessage(
        Guid tenantId,
        Guid companyId,
        Guid conversationId,
        bool isInbound,
        string messageType,
        string content,
        string? externalMessageId = null,
        bool generatedByAi = false) : base(tenantId)
    {
        CompanyId = companyId;
        WhatsAppConversationId = conversationId;
        IsInbound = isInbound;
        UpdateMessageType(messageType);
        UpdateContent(content);
        ExternalMessageId = NormalizeOptionalValue(externalMessageId, 180);
        GeneratedByAi = generatedByAi;
        Status = isInbound ? "RECEIVED" : "PENDING";
    }

    public Guid CompanyId { get; private set; }
    public Guid WhatsAppConversationId { get; private set; }
    public bool IsInbound { get; private set; }
    public string MessageType { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public string? ExternalMessageId { get; private set; }
    public string Status { get; private set; } = null!;
    public bool GeneratedByAi { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    public Company Company { get; private set; } = null!;
    public WhatsAppConversation Conversation { get; private set; } = null!;

    public void AttachExternalMessageId(string? externalMessageId)
    {
        ExternalMessageId = NormalizeOptionalValue(externalMessageId, 180);
        Touch();
    }

    public void MarkFailed()
    {
        Status = "FAILED";
        Touch();
    }

    public void UpdateStatus(string status, DateTime occurredAtUtc)
    {
        Status = NormalizeOptionalValue(status, 40) ?? Status;

        if (string.Equals(Status, "RECEIVED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Status, "DELIVERED", StringComparison.OrdinalIgnoreCase))
        {
            DeliveredAtUtc = occurredAtUtc;
        }

        if (string.Equals(Status, "READ", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Status, "READ_BY_ME", StringComparison.OrdinalIgnoreCase))
        {
            ReadAtUtc = occurredAtUtc;
        }

        Touch();
    }

    private void UpdateMessageType(string messageType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        MessageType = messageType.Trim();
        Touch();
    }

    private void UpdateContent(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var normalized = content.Trim();
        if (normalized.Length > 4000)
        {
            normalized = $"{normalized[..3997]}...";
        }

        Content = normalized;
        Touch();
    }

    private static string? NormalizeOptionalValue(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}
