using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class DeliveryFreightService : IDeliveryFreightService
{
    private readonly ZeroPaperDbContext _context;
    private readonly IReadOnlyDictionary<string, IDeliveryDistanceProvider> _providers;
    private readonly DeliveryDistanceOptions _options;

    public DeliveryFreightService(
        ZeroPaperDbContext context,
        IEnumerable<IDeliveryDistanceProvider> providers,
        IOptions<DeliveryDistanceOptions> options)
    {
        _context = context;
        _providers = providers.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
    }

    public DeliveryFreightSettingsDto BuildSettings(Company company)
    {
        var provider = ResolveProvider();
        return new DeliveryFreightSettingsDto
        {
            IsEnabled = company.EnableDeliveryFreight,
            OriginPostalCode = company.DeliveryOriginPostalCode,
            PricePerKm = company.DeliveryFreightPricePerKm,
            BaseFee = company.DeliveryFreightBaseFee,
            BaseDistanceKm = company.DeliveryFreightBaseDistanceKm,
            Provider = provider.Name,
            ProviderConfigured = provider.IsConfigured,
            IsTestMode = provider.IsTestMode,
            CacheDays = GetCacheDays(),
            PickupEstimatedMinutes = company.PickupEstimatedMinutes,
            DeliveryEstimatedMinutes = company.DeliveryEstimatedMinutes
        };
    }

    public async Task<DeliveryFreightQuoteDto> QuoteAsync(
        Company company,
        string? destinationPostalCode,
        decimal subtotal,
        CancellationToken cancellationToken = default)
    {
        var provider = ResolveProvider();
        var normalizedDestinationPostalCode = NormalizePostalCode(destinationPostalCode);
        var normalizedOriginPostalCode = NormalizePostalCode(company.DeliveryOriginPostalCode);
        var safeSubtotal = Math.Max(0, subtotal);

        var baseQuote = new DeliveryFreightQuoteDto
        {
            IsEnabled = company.EnableDeliveryFreight,
            IsConfigured = company.EnableDeliveryFreight &&
                           !string.IsNullOrWhiteSpace(normalizedOriginPostalCode) &&
                           company.DeliveryFreightPricePerKm > 0 &&
                           provider.IsConfigured,
            IsAvailable = false,
            IsTestMode = provider.IsTestMode,
            Provider = provider.Name,
            OriginPostalCode = normalizedOriginPostalCode,
            DestinationPostalCode = normalizedDestinationPostalCode,
            BaseFee = company.DeliveryFreightBaseFee,
            BaseDistanceKm = company.DeliveryFreightBaseDistanceKm,
            ChargedDistanceKm = 0,
            PricePerKm = company.DeliveryFreightPricePerKm,
            TotalWithFreight = safeSubtotal
        };

        if (!company.EnableDeliveryFreight)
        {
            baseQuote.Message = "Frete automatico desativado para esta unidade.";
            return baseQuote;
        }

        if (string.IsNullOrWhiteSpace(normalizedOriginPostalCode))
        {
            baseQuote.Message = "A unidade ainda nao configurou o CEP de origem.";
            return baseQuote;
        }

        if (company.DeliveryFreightPricePerKm <= 0)
        {
            baseQuote.Message = "A unidade ainda nao configurou o valor por KM.";
            return baseQuote;
        }

        if (string.IsNullOrWhiteSpace(normalizedDestinationPostalCode))
        {
            baseQuote.Message = "Informe o CEP da entrega para calcular o frete.";
            return baseQuote;
        }

        if (!provider.IsConfigured)
        {
            baseQuote.Message = "O provedor de mapas nao esta configurado no backend.";
            return baseQuote;
        }

        var (distanceKm, fromCache) = await GetDistanceKmAsync(
            company,
            normalizedOriginPostalCode,
            normalizedDestinationPostalCode,
            provider,
            cancellationToken);

        var baseDistanceKm = Math.Max(0, company.DeliveryFreightBaseDistanceKm);
        var billingDistanceKm = RoundDistanceUpForBilling(distanceKm);
        var chargedDistanceKm = RoundDistanceUpForBilling(Math.Max(0, billingDistanceKm - baseDistanceKm));
        var freightAmount = decimal.Round(company.DeliveryFreightBaseFee + (chargedDistanceKm * company.DeliveryFreightPricePerKm), 2);

        baseQuote.IsAvailable = true;
        baseQuote.DistanceKm = distanceKm;
        baseQuote.BaseDistanceKm = baseDistanceKm;
        baseQuote.ChargedDistanceKm = chargedDistanceKm;
        baseQuote.FreightAmount = freightAmount;
        baseQuote.TotalWithFreight = safeSubtotal + freightAmount;
        baseQuote.FromCache = fromCache;
        baseQuote.Message = provider.IsTestMode
            ? "Frete calculado em modo de teste, sem chamada paga de mapas."
            : provider.Name.Equals("approximate", StringComparison.OrdinalIgnoreCase)
                ? fromCache
                    ? "Frete aproximado por CEP reutilizado do cache."
                    : "Frete estimado por CEP com distancia aproximada."
                : fromCache
                    ? "Frete reutilizado do cache para evitar chamada desnecessaria."
                    : "Frete calculado pelo provedor de mapas.";

        return baseQuote;
    }

    private async Task<(decimal DistanceKm, bool FromCache)> GetDistanceKmAsync(
        Company company,
        string originPostalCode,
        string destinationPostalCode,
        IDeliveryDistanceProvider provider,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var cacheProviderName = GetDistanceCacheProviderName(provider);
        var cachedDistance = await _context.DeliveryDistanceCaches
            .FirstOrDefaultAsync(
                item => item.CompanyId == company.Id &&
                        item.Provider == cacheProviderName &&
                        item.OriginPostalCode == originPostalCode &&
                        item.DestinationPostalCode == destinationPostalCode &&
                        item.ExpiresAtUtc > nowUtc,
                cancellationToken);

        if (cachedDistance is not null)
        {
            return (cachedDistance.DistanceKm, true);
        }

        var distanceResult = await provider.CalculateDistanceAsync(originPostalCode, destinationPostalCode, cancellationToken);
        var expiresAtUtc = nowUtc.AddDays(GetCacheDays());

        var existingDistance = await _context.DeliveryDistanceCaches
            .FirstOrDefaultAsync(
                item => item.CompanyId == company.Id &&
                        item.Provider == cacheProviderName &&
                        item.OriginPostalCode == originPostalCode &&
                        item.DestinationPostalCode == destinationPostalCode,
                cancellationToken);

        if (existingDistance is null)
        {
            await _context.DeliveryDistanceCaches.AddAsync(
                new DeliveryDistanceCache(
                    company.TenantId,
                    company.Id,
                    originPostalCode,
                    destinationPostalCode,
                    cacheProviderName,
                    distanceResult.DistanceKm,
                    expiresAtUtc),
                cancellationToken);
        }
        else
        {
            existingDistance.UpdateDistance(distanceResult.DistanceKm, expiresAtUtc);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return (distanceResult.DistanceKm, false);
    }

    private static string GetDistanceCacheProviderName(IDeliveryDistanceProvider provider)
    {
        return provider.Name.Equals("approximate", StringComparison.OrdinalIgnoreCase)
            ? "approximate-address-v2"
            : provider.Name;
    }

    private IDeliveryDistanceProvider ResolveProvider()
    {
        var configuredProvider = string.IsNullOrWhiteSpace(_options.DistanceProvider)
            ? DeliveryDistanceOptions.DefaultProvider
            : _options.DistanceProvider.Trim();

        return _providers.TryGetValue(configuredProvider, out var provider)
            ? provider
            : _providers[DeliveryDistanceOptions.DefaultProvider];
    }

    private int GetCacheDays()
    {
        return Math.Clamp(_options.CacheDays, 1, 180);
    }

    private static decimal RoundDistanceUpForBilling(decimal distanceKm)
    {
        if (distanceKm <= 0)
        {
            return 0;
        }

        return decimal.Ceiling(distanceKm);
    }

    private static string? NormalizePostalCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
        {
            return null;
        }

        return digits;
    }
}
