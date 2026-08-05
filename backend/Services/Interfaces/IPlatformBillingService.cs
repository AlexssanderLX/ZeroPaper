using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IPlatformBillingService
{
    Task<AdminPlatformBillingStatusDto> GetStatusAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<AdminPlatformBillingStatusDto> ConfigureAsync(WorkspaceSessionContext session, ConfigureAdminPlatformBillingRequestDto request, CancellationToken cancellationToken = default);
    Task DisconnectAsync(WorkspaceSessionContext session, AdminSensitiveActionRequestDto request, CancellationToken cancellationToken = default);
    Task<AdminSubscriptionCheckoutDto> CreateSubscriptionCheckoutAsync(WorkspaceSessionContext session, Guid companyId, AdminSensitiveActionRequestDto request, CancellationToken cancellationToken = default);
    Task<AdminSubscriptionCheckoutDto> SyncSubscriptionAsync(WorkspaceSessionContext session, Guid companyId, CancellationToken cancellationToken = default);
}
