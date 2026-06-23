using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetDashboardAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<AdminCompanyMasterPasswordRevealDto> RevealCompanyMasterPasswordAsync(
        WorkspaceSessionContext session,
        Guid companyId,
        AdminSensitiveActionRequestDto request,
        CancellationToken cancellationToken = default);
    Task<AdminCompanyMasterPasswordRevealDto> RotateCompanyMasterPasswordAsync(
        WorkspaceSessionContext session,
        Guid companyId,
        AdminSensitiveActionRequestDto request,
        CancellationToken cancellationToken = default);
    Task<AdminCompanyPlanUpdateDto> UpdateCompanyPlanAsync(
        WorkspaceSessionContext session,
        Guid companyId,
        UpdateAdminCompanyPlanRequestDto request,
        CancellationToken cancellationToken = default);
    Task DeleteCompanyAsync(
        WorkspaceSessionContext session,
        Guid companyId,
        DeleteAdminCompanyRequestDto request,
        CancellationToken cancellationToken = default);
}
