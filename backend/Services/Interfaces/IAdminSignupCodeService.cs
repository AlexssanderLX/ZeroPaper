using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IAdminSignupCodeService
{
    Task<IReadOnlyList<SignupCodeDto>> GetSignupCodesAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<CreateSignupCodeResponseDto> CreateSignupCodeAsync(WorkspaceSessionContext session, CreateSignupCodeRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteSignupCodeAsync(WorkspaceSessionContext session, Guid codeId, CancellationToken cancellationToken = default);
    Task<CleanupSignupCodesResponseDto> CleanupSignupCodesAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
}
