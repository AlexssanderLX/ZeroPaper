using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IAdminOwnerService
{
    Task<IReadOnlyList<AdminOwnerDto>> GetOwnersAsync(
        WorkspaceSessionContext session,
        CancellationToken cancellationToken = default);

    Task<AdminOwnerDto?> GetOwnerByIdAsync(
        WorkspaceSessionContext session,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<AdminOwnerDto> CreateOwnerAsync(
        WorkspaceSessionContext session,
        CreateAdminOwnerRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AdminOwnerDto> UpdateOwnerAsync(
        WorkspaceSessionContext session,
        Guid ownerId,
        UpdateAdminOwnerRequestDto request,
        CancellationToken cancellationToken = default);

    Task ResetOwnerPasswordAsync(
        WorkspaceSessionContext session,
        Guid ownerId,
        ResetAdminOwnerPasswordRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AdminOwnerDto> DeactivateOwnerAsync(
        WorkspaceSessionContext session,
        Guid ownerId,
        ChangeAdminOwnerStatusRequestDto request,
        CancellationToken cancellationToken = default);

    Task<AdminOwnerDto> ReactivateOwnerAsync(
        WorkspaceSessionContext session,
        Guid ownerId,
        ChangeAdminOwnerStatusRequestDto request,
        CancellationToken cancellationToken = default);

    Task HardDeleteOwnerAsync(
        WorkspaceSessionContext session,
        Guid ownerId,
        HardDeleteAdminOwnerRequestDto request,
        CancellationToken cancellationToken = default);
}
