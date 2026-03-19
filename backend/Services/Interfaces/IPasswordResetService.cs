using ZeroPaper.DTOs.Auth;

namespace ZeroPaper.Services.Interfaces;

public interface IPasswordResetService
{
    Task<PasswordResetRequestResponseDto> RequestAsync(PasswordResetRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> ResetAsync(ResetPasswordDto request, CancellationToken cancellationToken = default);
}
