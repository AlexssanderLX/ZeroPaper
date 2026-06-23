using System.Text.Json;
using ZeroPaper.DTOs.Workspace;

namespace ZeroPaper.Services.Interfaces;

public interface IWhatsAppIntegrationService
{
    Task HandleReceiveWebhookAsync(string instanceId, string? key, JsonDocument payload, CancellationToken cancellationToken = default);
    Task HandleMessageStatusWebhookAsync(string instanceId, string? key, JsonDocument payload, CancellationToken cancellationToken = default);
    Task HandleConnectedWebhookAsync(string instanceId, string? key, JsonDocument payload, CancellationToken cancellationToken = default);
    Task HandleDisconnectedWebhookAsync(string instanceId, string? key, JsonDocument payload, CancellationToken cancellationToken = default);
    Task HandleEvolutionWebhookAsync(string instanceName, string? key, JsonDocument payload, CancellationToken cancellationToken = default);
    Task<WhatsAppConnectionSnapshotDto> PrepareEvolutionConnectionAsync(
        Guid companyId,
        string? phoneNumber = null,
        bool forceNewSession = false,
        CancellationToken cancellationToken = default);
    Task TrySendDeliveryOrderConfirmationAsync(Guid orderId, bool isUpdate, CancellationToken cancellationToken = default);
}
