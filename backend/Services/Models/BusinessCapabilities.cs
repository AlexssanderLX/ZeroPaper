using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Services.Models;

public sealed record BusinessCapabilities
{
    public bool HasCustomerProfiles { get; init; }
    public bool HasCatalog { get; init; }
    public bool HasPets { get; init; }
    public bool HasAppointments { get; init; }
    public bool HasOnlinePayments { get; init; }
    public bool HasWhatsApp { get; init; }
    public bool HasAiAssistant { get; init; }
    public bool HasCoupons { get; init; }
    public bool HasReports { get; init; }
    public bool HasPrinting { get; init; }
    public bool HasDelivery { get; init; }
    public bool HasTables { get; init; }
    public bool HasKitchen { get; init; }
    public bool HasWaiterCalls { get; init; }

    public static BusinessCapabilities Resolve(
        BusinessSegment segment,
        bool includesMenu,
        bool includesTables,
        bool includesKitchen,
        bool includesCash,
        bool includesDelivery,
        bool includesPrinting,
        bool includesWaiterCalls,
        bool includesAiAssistant,
        bool hasCoupons,
        bool hasReports)
    {
        var isRestaurant = segment == BusinessSegment.Restaurant;
        var isPetShop = segment == BusinessSegment.PetShop;

        return new BusinessCapabilities
        {
            HasCustomerProfiles = includesCash || includesDelivery || isPetShop,
            HasCatalog = includesMenu,
            HasPets = isPetShop,
            HasAppointments = isPetShop,
            HasOnlinePayments = includesCash,
            HasWhatsApp = includesAiAssistant,
            HasAiAssistant = includesAiAssistant,
            HasCoupons = hasCoupons,
            HasReports = hasReports,
            HasPrinting = includesPrinting,
            HasDelivery = isRestaurant && includesDelivery,
            HasTables = isRestaurant && includesTables,
            HasKitchen = isRestaurant && includesKitchen,
            HasWaiterCalls = isRestaurant && includesWaiterCalls
        };
    }
}

public static class BusinessCapabilityGuard
{
    public static void Require(bool enabled, string capability)
    {
        if (!enabled)
        {
            throw new CapabilityUnavailableException($"A capacidade {capability} nao esta disponivel para esta empresa.");
        }
    }
}

public sealed class CapabilityUnavailableException : InvalidOperationException
{
    public CapabilityUnavailableException(string message) : base(message) { }
}
