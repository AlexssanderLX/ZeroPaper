using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Services.Models;

public sealed record BusinessCapabilities
{
    public bool HasCustomerProfiles { get; init; }
    public bool HasCatalog { get; init; }
    public bool HasPets { get; init; }
    public bool HasAppointments { get; init; }
    public bool HasPublicBooking { get; init; }
    public bool HasBoarding { get; init; }
    public bool HasPublicBoardingRequest { get; init; }
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
        SubscriptionProductType productType,
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
        var isPetFamily = segment == BusinessSegment.PetShop;
        var hasPetShopProduct = isPetFamily && productType == SubscriptionProductType.PetShop;
        var hasHostingProduct = isPetFamily && productType == SubscriptionProductType.PetHosting;

        return new BusinessCapabilities
        {
            HasCustomerProfiles = includesCash || includesDelivery || hasPetShopProduct || hasHostingProduct,
            HasCatalog = includesMenu && (isRestaurant || hasPetShopProduct),
            HasPets = hasPetShopProduct || hasHostingProduct,
            HasAppointments = hasPetShopProduct,
            HasPublicBooking = hasPetShopProduct,
            HasBoarding = hasHostingProduct,
            HasPublicBoardingRequest = hasHostingProduct,
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
        bool hasReports) => Resolve(
            segment,
            segment == BusinessSegment.PetShop
                ? SubscriptionProductType.PetShop
                : SubscriptionProductType.Restaurant,
            includesMenu,
            includesTables,
            includesKitchen,
            includesCash,
            includesDelivery,
            includesPrinting,
            includesWaiterCalls,
            includesAiAssistant,
            hasCoupons,
            hasReports);
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
