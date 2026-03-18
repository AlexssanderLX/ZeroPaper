namespace ZeroPaper.DTOs.Onboarding;

public class RestaurantOnboardingResponseDto
{
    public string TenantIdentifier { get; init; } = string.Empty;
    public string AccessSlug { get; init; } = string.Empty;
    public string AccessUrl { get; init; } = string.Empty;
    public string OwnerEmail { get; init; } = string.Empty;
    public string PlanName { get; init; } = string.Empty;
}
