namespace ZeroPaper.DTOs.Workspace;

public class AiAssistantSettingsDto
{
    public string UnitDisplayName { get; set; } = string.Empty;
    public bool ApiConfigured { get; set; }
    public bool WhatsAppServerConfigured { get; set; }
    public bool IsEnabled { get; set; }
    public string Model { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string GreetingMessage { get; set; } = string.Empty;
    public string RedirectMessage { get; set; } = string.Empty;
    public string FallbackMessage { get; set; } = string.Empty;
    public string? OrderingLink { get; set; }
    public string? PixReceiverName { get; set; }
    public string? PixKey { get; set; }
    public string? PixMessage { get; set; }
    public List<int>? ServiceDays { get; set; }
    public string? ServiceStartTime { get; set; }
    public string? ServiceEndTime { get; set; }
    public int MaxOutputTokens { get; set; }
    public bool WhatsAppEnabled { get; set; }
    public bool WhatsAppConfigured { get; set; }
    public string? WhatsAppInstanceId { get; set; }
    public string? WhatsAppInstanceTokenMasked { get; set; }
    public bool HasWhatsAppAccountSecurityToken { get; set; }
    public bool IsWhatsAppConnected { get; set; }
    public string? WhatsAppConnectedPhone { get; set; }
    public DateTime? WhatsAppConnectedAtUtc { get; set; }
    public DateTime? WhatsAppDisconnectedAtUtc { get; set; }
    public DateTime? WhatsAppLastIncomingAtUtc { get; set; }
    public DateTime? WhatsAppLastOutgoingAtUtc { get; set; }
    public string? WhatsAppWebhookReceiveUrl { get; set; }
    public string? WhatsAppWebhookMessageStatusUrl { get; set; }
    public string? WhatsAppWebhookConnectedUrl { get; set; }
    public string? WhatsAppWebhookDisconnectedUrl { get; set; }
    public List<WhatsAppConversationSummaryDto> RecentWhatsAppConversations { get; set; } = [];
}

public class UpdateAiAssistantSettingsRequestDto
{
    public bool IsEnabled { get; set; }
    public string? Model { get; set; }
    public string? SystemPrompt { get; set; }
    public string? GreetingMessage { get; set; }
    public string? RedirectMessage { get; set; }
    public string? FallbackMessage { get; set; }
    public string? OrderingLink { get; set; }
    public string? PixReceiverName { get; set; }
    public string? PixKey { get; set; }
    public string? PixMessage { get; set; }
    public List<int>? ServiceDays { get; set; }
    public string? ServiceStartTime { get; set; }
    public string? ServiceEndTime { get; set; }
    public int MaxOutputTokens { get; set; }
    public bool WhatsAppEnabled { get; set; }
    public string? WhatsAppInstanceId { get; set; }
    public string? NewWhatsAppInstanceToken { get; set; }
    public string? NewWhatsAppAccountSecurityToken { get; set; }
}

public class UpdateAiAssistantQuickStatusRequestDto
{
    public bool IsEnabled { get; set; }
}

public class AiAssistantQuickStatusDto
{
    public bool IsEnabled { get; set; }
    public bool IsConfigured { get; set; }
}

public class AiAssistantTestRequestDto
{
    public string Message { get; set; } = string.Empty;
}

public class PrepareWhatsAppConnectionRequestDto
{
    public string? PhoneNumber { get; set; }
    public bool ForceNewSession { get; set; }
}

public class AiAssistantTestResponseDto
{
    public string Reply { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }
}

public class AiAssistantConversationTurnDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class AiAssistantGeneratedReplyDto
{
    public string Reply { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }
}

public class WhatsAppConnectionSnapshotDto
{
    public bool ServerConfigured { get; set; }
    public bool InstanceConfigured { get; set; }
    public string InstanceName { get; set; } = string.Empty;
    public string? State { get; set; }
    public bool IsConnected { get; set; }
    public string? ConnectedPhone { get; set; }
    public string? QrCodeBase64 { get; set; }
    public string? QrCodeText { get; set; }
    public string? PairingCode { get; set; }
    public string? Message { get; set; }
}

public class WhatsAppConversationSummaryDto
{
    public Guid Id { get; set; }
    public string ExternalPhone { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string LastMessagePreview { get; set; } = string.Empty;
    public string LastDirection { get; set; } = string.Empty;
    public DateTime? LastIncomingAtUtc { get; set; }
    public DateTime? LastOutgoingAtUtc { get; set; }
    public DateTime? LastInteractionAtUtc { get; set; }
    public int MessageCount { get; set; }
}
