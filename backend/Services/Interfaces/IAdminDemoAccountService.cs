using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IAdminDemoAccountService
{
    Task<AdminDemoAccountDto> GetAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<AdminDemoAccountDto> EnsureAccountAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<AdminDemoLinkCreatedDto> RotateLinkAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<AdminDemoAccountDto> RevokeLinkAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<AdminDemoAccountDto> RevokeSessionsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
}
