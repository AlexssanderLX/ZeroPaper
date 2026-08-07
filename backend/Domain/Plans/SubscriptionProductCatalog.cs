using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Plans;

public sealed record SubscriptionProductDefinition(
    SubscriptionProductType Type,
    string Key,
    string Name,
    decimal MonthlyPrice,
    int DefaultMaxUsers);

public static class SubscriptionProductCatalog
{
    public static readonly SubscriptionProductDefinition PetShop = new(
        SubscriptionProductType.PetShop,
        "pet-shop",
        "ZeroPaper Pet Shop",
        150m,
        5);

    public static readonly SubscriptionProductDefinition PetHosting = new(
        SubscriptionProductType.PetHosting,
        "pet-hospedagem",
        "ZeroPaper Hospedagem",
        100m,
        5);

    public static readonly IReadOnlyList<SubscriptionProductDefinition> PetProducts =
    [
        PetShop,
        PetHosting
    ];

    public static SubscriptionProductDefinition ResolvePet(string? key)
    {
        var normalized = key?.Trim().ToLowerInvariant();
        return PetProducts.FirstOrDefault(product => product.Key == normalized)
            ?? throw new ArgumentException("Produto invalido para o segmento Pet.", nameof(key));
    }

    public static SubscriptionProductDefinition Resolve(SubscriptionProductType type) => type switch
    {
        SubscriptionProductType.PetShop => PetShop,
        SubscriptionProductType.PetHosting => PetHosting,
        _ => throw new ArgumentException("O produto informado nao pertence ao catalogo Pet.", nameof(type))
    };
}
