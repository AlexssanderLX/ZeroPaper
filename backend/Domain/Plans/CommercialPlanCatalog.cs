using System.Globalization;
using System.Text;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Plans;

public sealed record CommercialPlanFeatures(
    bool HasWhatsAppAI,
    bool HasDelivery,
    bool HasAutoPrint,
    bool HasBasicReports,
    bool HasManagementDashboard,
    bool HasAdvancedReports,
    bool HasCoupons,
    bool HasRecurringCustomers);

public sealed record CommercialPlanDefinition(
    CommercialPlanTier Tier,
    string Key,
    string Name,
    decimal MonthlyPrice,
    int DefaultMaxUsers,
    bool IncludesMenuModule,
    bool IncludesTablesModule,
    bool IncludesKitchenModule,
    bool IncludesCashModule,
    bool IncludesStockModule,
    bool IncludesDeliveryModule,
    bool IncludesPrintingModule,
    bool IncludesWaiterCallModule,
    bool IncludesAiAssistantModule,
    CommercialPlanFeatures Features);

public static class CommercialPlanCatalog
{
    public const string EssentialKey = "essencial";
    public const string OperationKey = "operacao";
    public const string ManagementKey = "gestao";
    public const string CustomPlanName = "ZeroPaper Personalizado";

    public static readonly CommercialPlanDefinition Essential = new(
        CommercialPlanTier.Essential,
        EssentialKey,
        "ZeroPaper Essencial",
        80m,
        3,
        IncludesMenuModule: true,
        IncludesTablesModule: true,
        IncludesKitchenModule: true,
        IncludesCashModule: true,
        IncludesStockModule: false,
        IncludesDeliveryModule: false,
        IncludesPrintingModule: true,
        IncludesWaiterCallModule: true,
        IncludesAiAssistantModule: false,
        new CommercialPlanFeatures(
            HasWhatsAppAI: false,
            HasDelivery: false,
            HasAutoPrint: false,
            HasBasicReports: false,
            HasManagementDashboard: false,
            HasAdvancedReports: false,
            HasCoupons: false,
            HasRecurringCustomers: false));

    public static readonly CommercialPlanDefinition Operation = new(
        CommercialPlanTier.Operation,
        OperationKey,
        "ZeroPaper Operacao",
        120m,
        5,
        IncludesMenuModule: true,
        IncludesTablesModule: true,
        IncludesKitchenModule: true,
        IncludesCashModule: true,
        IncludesStockModule: false,
        IncludesDeliveryModule: true,
        IncludesPrintingModule: true,
        IncludesWaiterCallModule: true,
        IncludesAiAssistantModule: true,
        new CommercialPlanFeatures(
            HasWhatsAppAI: true,
            HasDelivery: true,
            HasAutoPrint: true,
            HasBasicReports: true,
            HasManagementDashboard: false,
            HasAdvancedReports: false,
            HasCoupons: false,
            HasRecurringCustomers: false));

    public static readonly CommercialPlanDefinition Management = new(
        CommercialPlanTier.Management,
        ManagementKey,
        "ZeroPaper Gestao",
        180m,
        8,
        IncludesMenuModule: true,
        IncludesTablesModule: true,
        IncludesKitchenModule: true,
        IncludesCashModule: true,
        IncludesStockModule: false,
        IncludesDeliveryModule: true,
        IncludesPrintingModule: true,
        IncludesWaiterCallModule: true,
        IncludesAiAssistantModule: true,
        new CommercialPlanFeatures(
            HasWhatsAppAI: true,
            HasDelivery: true,
            HasAutoPrint: true,
            HasBasicReports: true,
            HasManagementDashboard: true,
            HasAdvancedReports: true,
            HasCoupons: true,
            HasRecurringCustomers: true));

    public static readonly IReadOnlyList<CommercialPlanDefinition> StandardPlans =
    [
        Essential,
        Operation,
        Management
    ];

    public static CommercialPlanDefinition Resolve(string? planName)
    {
        return TryResolve(planName, out var plan) ? plan : Operation;
    }

    public static bool TryResolve(string? planName, out CommercialPlanDefinition plan)
    {
        var normalized = Normalize(planName);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            plan = Operation;
            return false;
        }

        if (normalized.Contains("premium", StringComparison.Ordinal) ||
            normalized.Contains("gestao", StringComparison.Ordinal))
        {
            plan = Management;
            return true;
        }

        if (normalized.Contains("pro", StringComparison.Ordinal) ||
            normalized.Contains("vendas", StringComparison.Ordinal) ||
            normalized.Contains("ia", StringComparison.Ordinal) ||
            normalized.Contains("operacao", StringComparison.Ordinal))
        {
            plan = Operation;
            return true;
        }

        if (normalized.Contains("basico", StringComparison.Ordinal) ||
            normalized.Contains("essencial", StringComparison.Ordinal))
        {
            plan = Essential;
            return true;
        }

        plan = Operation;
        return false;
    }

    public static CommercialPlanFeatures ResolveFeatures(string? planName)
    {
        return TryResolve(planName, out var plan)
            ? plan.Features
            : new CommercialPlanFeatures(
                HasWhatsAppAI: false,
                HasDelivery: false,
                HasAutoPrint: false,
                HasBasicReports: false,
                HasManagementDashboard: false,
                HasAdvancedReports: false,
                HasCoupons: false,
                HasRecurringCustomers: false);
    }

    public static CommercialPlanTier ResolveTier(string? planName)
    {
        return TryResolve(planName, out var plan) ? plan.Tier : CommercialPlanTier.Custom;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
            }
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
