namespace ZeroPaper.DTOs.Onboarding;

using ZeroPaper.Domain.Enums;

public class RestaurantOnboardingResponseDto
{
    public string TenantIdentifier { get; init; } = string.Empty;
    public string AccessSlug { get; init; } = string.Empty;
    public string AccessUrl { get; init; } = string.Empty;
    public string OwnerEmail { get; init; } = string.Empty;
    public string PlanName { get; init; } = string.Empty;
    public string PlanKey { get; init; } = string.Empty;
    public int BusinessSegment { get; init; }
    public SubscriptionProductType ProductType { get; init; } = SubscriptionProductType.Restaurant;
    public bool RequiresApproval { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? CheckoutUrl { get; init; }
}
