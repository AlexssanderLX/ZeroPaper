using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class GoogleRoutesDeliveryDistanceProvider : IDeliveryDistanceProvider
{
    private const string DefaultBaseUrl = "https://routes.googleapis.com/";
    private readonly HttpClient _httpClient;
    private readonly DeliveryDistanceOptions _options;

    public GoogleRoutesDeliveryDistanceProvider(HttpClient httpClient, IOptions<DeliveryDistanceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string Name => "google";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ResolveApiKey());
    public bool IsTestMode => false;

    public async Task<DeliveryDistanceResult> CalculateDistanceAsync(
        string originPostalCode,
        string destinationPostalCode,
        CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("A chave do Google Maps nao esta configurada no backend.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "directions/v2:computeRoutes")
        {
            Content = JsonContent.Create(new
            {
                origin = new
                {
                    address = $"{FormatPostalCode(originPostalCode)}, Brasil"
                },
                destination = new
                {
                    address = $"{FormatPostalCode(destinationPostalCode)}, Brasil"
                },
                travelMode = "DRIVE",
                routingPreference = "TRAFFIC_UNAWARE",
                languageCode = "pt-BR",
                units = "METRIC"
            })
        };

        request.Headers.TryAddWithoutValidation("X-Goog-Api-Key", apiKey.Trim());
        request.Headers.TryAddWithoutValidation("X-Goog-FieldMask", "routes.distanceMeters");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Nao foi possivel calcular a distancia no provedor de mapas. Status {(int)response.StatusCode}: {body}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var routes = document.RootElement.GetProperty("routes");

        if (routes.GetArrayLength() == 0 ||
            !routes[0].TryGetProperty("distanceMeters", out var distanceElement))
        {
            throw new InvalidOperationException("O provedor de mapas nao retornou uma distancia para este CEP.");
        }

        var distanceMeters = distanceElement.GetDecimal();
        var distanceKm = decimal.Round(distanceMeters / 1000m, 2);
        return new DeliveryDistanceResult(distanceKm, Name, IsTestMode);
    }

    private string? ResolveApiKey()
    {
        return string.IsNullOrWhiteSpace(_options.GoogleMaps.ApiKey)
            ? Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY")
            : _options.GoogleMaps.ApiKey;
    }

    private static string FormatPostalCode(string postalCode)
    {
        return postalCode.Length == 8
            ? $"{postalCode[..5]}-{postalCode[5..]}"
            : postalCode;
    }
}
