using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IMercadoPagoService
{
    Task<MercadoPagoStatusDto> GetStatusAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);

    Task<MercadoPagoConnectResponseDto> StartConnectionAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);

    Task<bool> HandleOAuthCallbackAsync(string? state, string? code, CancellationToken cancellationToken = default);

    Task DisconnectAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);

    Task<MercadoPagoCheckoutResponseDto> CreatePublicCheckoutAsync(
        string publicCode,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<bool> HandlePaymentNotificationAsync(string? paymentId, Guid? orderId, CancellationToken cancellationToken = default);
}
