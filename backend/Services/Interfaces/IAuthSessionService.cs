using ZeroPaper.DTOs.Auth;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IAuthSessionService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<WorkspaceSessionContext?> GetSessionAsync(string? authorizationHeader, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? authorizationHeader, CancellationToken cancellationToken = default);
}

