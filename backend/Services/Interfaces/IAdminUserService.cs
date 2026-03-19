using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<AdminUserDto> DeactivateUserAsync(WorkspaceSessionContext session, Guid userId, CancellationToken cancellationToken = default);
    Task<AdminUserDto> ReactivateUserAsync(WorkspaceSessionContext session, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(WorkspaceSessionContext session, Guid userId, CancellationToken cancellationToken = default);
}
