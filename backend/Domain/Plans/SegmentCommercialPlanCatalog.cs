using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Plans;

public static class SegmentCommercialPlanCatalog
{
    private static readonly IReadOnlyDictionary<BusinessSegment, IReadOnlyList<CommercialPlanDefinition>> Plans =
        new Dictionary<BusinessSegment, IReadOnlyList<CommercialPlanDefinition>>
        {
            [BusinessSegment.Restaurant] = CommercialPlanCatalog.StandardPlans,
            [BusinessSegment.PetShop] =
            [
                CreatePetPlan(
                    CommercialPlanTier.Essential, "essencial", "ZeroPaper Pet Essencial", 80m, 3,
                    ai: false,
                    new(false, false, false, false, false, false, false, true, false)),
                CreatePetPlan(
                    CommercialPlanTier.Operation, "operacao", "ZeroPaper Pet Operacao", 120m, 5,
                    ai: true,
                    new(true, false, false, true, false, false, false, true, false)),
                CreatePetPlan(
                    CommercialPlanTier.Management, "gestao", "ZeroPaper Pet Gestao", 180m, 8,
                    ai: true,
                    new(true, false, false, true, true, true, true, true, false))
            ]
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

    private static CommercialPlanDefinition CreatePetPlan(
        CommercialPlanTier tier,
        string key,
        string name,
        decimal price,
        int users,
        bool ai,
        CommercialPlanFeatures features) => new(
            tier, key, name, price, users,
            IncludesMenuModule: true,
            IncludesTablesModule: false,
            IncludesKitchenModule: false,
            IncludesCashModule: true,
            IncludesStockModule: false,
            IncludesDeliveryModule: false,
            IncludesPrintingModule: false,
            IncludesWaiterCallModule: false,
            IncludesAiAssistantModule: ai,
            features);
}
