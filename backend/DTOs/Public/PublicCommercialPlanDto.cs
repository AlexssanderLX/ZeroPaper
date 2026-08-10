using ZeroPaper.Domain.Enums;

namespace ZeroPaper.DTOs.Public;

public sealed class PublicCommercialPlanDto
{
    public BusinessSegment Segment { get; init; }
    public SubscriptionProductType ProductType { get; init; } = SubscriptionProductType.Restaurant;
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal MonthlyPrice { get; init; }
    public ProductReleaseStage ReleaseStage { get; init; } = ProductReleaseStage.Stable;
    public bool RequiresPayment { get; init; } = true;
    public int MaxUsers { get; init; }
    public bool Recommended { get; init; }
    public IReadOnlyList<string> Features { get; init; } = [];
}
