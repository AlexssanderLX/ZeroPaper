using ZeroPaper.DTOs.Public;

namespace ZeroPaper.Services.Interfaces;

public interface IAccessRequestNotificationService
{
    Task<AccessRequestResponseDto> SendAsync(AccessRequestDto request, CancellationToken cancellationToken = default);
}
