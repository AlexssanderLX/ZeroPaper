using ZeroPaper.DTOs.Onboarding;

namespace ZeroPaper.Services.Interfaces;

public interface IRestaurantOnboardingService
{
    Task<RestaurantOnboardingResponseDto> CreateAsync(
        RestaurantOnboardingRequestDto request,
        CancellationToken cancellationToken = default);
}
