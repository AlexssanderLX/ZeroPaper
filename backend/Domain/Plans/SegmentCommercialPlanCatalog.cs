using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Plans;

public static class SegmentCommercialPlanCatalog
{
    private static readonly IReadOnlyDictionary<BusinessSegment, IReadOnlyList<CommercialPlanDefinition>> Plans =
        new Dictionary<BusinessSegment, IReadOnlyList<CommercialPlanDefinition>>
        {
            [BusinessSegment.Restaurant] = CommercialPlanCatalog.StandardPlans
        };

    public static IReadOnlyList<CommercialPlanDefinition> GetPlans(BusinessSegment segment) =>
        Plans.TryGetValue(segment, out var plans)
            ? plans
            : throw new ArgumentException("Segmento indisponivel para cadastro.", nameof(segment));

    public static CommercialPlanDefinition Resolve(BusinessSegment segment, string? planKey)
    {
        var plans = GetPlans(segment);
        var normalizedKey = planKey?.Trim().ToLowerInvariant();
        return plans.FirstOrDefault(plan => plan.Key == normalizedKey)
            ?? throw new ArgumentException("Plano invalido para o segmento selecionado.", nameof(planKey));
    }
}
