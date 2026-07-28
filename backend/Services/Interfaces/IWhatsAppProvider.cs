using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Services.Interfaces;

public interface IWhatsAppProvider
{
    WhatsAppProvider Provider { get; }
    bool IsConfigured { get; }
    Task<WhatsAppProviderSendResult> SendTextAsync(
        Company company,
        string phone,
        string message,
        CancellationToken cancellationToken = default);
}

public sealed record WhatsAppProviderSendResult(
    bool Succeeded,
    string? ExternalMessageId,
    string Status)
{
    public static WhatsAppProviderSendResult Failed(string status)
        => new(false, null, status);
}
