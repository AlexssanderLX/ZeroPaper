using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class ApproximatePostalCodeDeliveryDistanceProvider : IDeliveryDistanceProvider
{
    private const string DefaultBaseUrl = "https://nominatim.openstreetmap.org/";
    private const string ViaCepBaseUrl = "https://viacep.com.br/ws/";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly DeliveryDistanceOptions _options;

    public ApproximatePostalCodeDeliveryDistanceProvider(
        HttpClient httpClient,
        IOptions<DeliveryDistanceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string Name => "approximate";
    public bool IsConfigured => true;
    public bool IsTestMode => false;

    public async Task<DeliveryDistanceResult> CalculateDistanceAsync(
        string originPostalCode,
        string destinationPostalCode,
        CancellationToken cancellationToken = default)
    {
        if (originPostalCode == destinationPostalCode)
        {
            return new DeliveryDistanceResult(GetMinimumDistanceKm(), Name, IsTestMode);
        }

        try
        {
            var origin = await GetCoordinatesAsync(originPostalCode, cancellationToken);
            var destination = await GetCoordinatesAsync(destinationPostalCode, cancellationToken);
            var straightLineKm = CalculateHaversineDistanceKm(origin.Latitude, origin.Longitude, destination.Latitude, destination.Longitude);
            var estimatedKm = decimal.Round(
                Math.Clamp(straightLineKm * GetRouteFactor(), GetMinimumDistanceKm(), GetMaximumDistanceKm()),
                2);

            return new DeliveryDistanceResult(estimatedKm, Name, IsTestMode);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            var fallbackKm = EstimateFallbackDistanceKm(originPostalCode, destinationPostalCode);
            return new DeliveryDistanceResult(fallbackKm, Name, IsTestMode);
        }
    }

    private async Task<(double Latitude, double Longitude)> GetCoordinatesAsync(string postalCode, CancellationToken cancellationToken)
    {
        var postalAddress = await TryGetPostalAddressAsync(postalCode, cancellationToken);
        if (postalAddress is not null)
        {
            foreach (var query in BuildAddressQueries(postalAddress, postalCode))
            {
                var coordinates = await TryGetCoordinatesByQueryAsync(query, postalAddress, cancellationToken);
                if (coordinates.HasValue)
                {
                    return coordinates.Value;
                }
            }
        }

        return await GetCoordinatesByPostalCodeAsync(postalCode, cancellationToken);
    }

    private async Task<(double Latitude, double Longitude)> GetCoordinatesByPostalCodeAsync(string postalCode, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"search?format=jsonv2&limit=1&countrycodes=br&postalcode={Uri.EscapeDataString(FormatPostalCode(postalCode))}");

        request.Headers.TryAddWithoutValidation("User-Agent", ResolveUserAgent());
        request.Headers.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Nao foi possivel localizar este CEP.");
        }

        var firstItem = document.RootElement[0];
        var latitude = ParseCoordinate(firstItem.GetProperty("lat").GetString());
        var longitude = ParseCoordinate(firstItem.GetProperty("lon").GetString());
        return (latitude, longitude);
    }

    private async Task<(double Latitude, double Longitude)?> TryGetCoordinatesByQueryAsync(
        string query,
        PostalCodeAddress postalAddress,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"search?format=jsonv2&limit=1&countrycodes=br&q={Uri.EscapeDataString(query)}");

            request.Headers.TryAddWithoutValidation("User-Agent", ResolveUserAgent());
            request.Headers.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
            {
                return null;
            }

            var firstItem = document.RootElement[0];
            var displayName = firstItem.TryGetProperty("display_name", out var displayNameElement)
                ? displayNameElement.GetString()
                : null;

            if (!LooksLikeAddressMatch(displayName, postalAddress))
            {
                return null;
            }

            var latitude = ParseCoordinate(firstItem.GetProperty("lat").GetString());
            var longitude = ParseCoordinate(firstItem.GetProperty("lon").GetString());
            return (latitude, longitude);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private async Task<PostalCodeAddress?> TryGetPostalAddressAsync(string postalCode, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri($"{ViaCepBaseUrl}{Uri.EscapeDataString(postalCode)}/json/", UriKind.Absolute));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var address = await JsonSerializer.DeserializeAsync<PostalCodeAddress>(stream, JsonOptions, cancellationToken);
            if (address is null || address.Erro || string.IsNullOrWhiteSpace(address.Localidade) || string.IsNullOrWhiteSpace(address.Uf))
            {
                return null;
            }

            return address;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> BuildAddressQueries(PostalCodeAddress address, string postalCode)
    {
        var cityStateCountry = $"{address.Localidade}, {address.Uf}, Brasil";

        if (!string.IsNullOrWhiteSpace(address.Logradouro))
        {
            if (!string.IsNullOrWhiteSpace(address.Bairro))
            {
                yield return $"{address.Logradouro}, {address.Bairro}, {cityStateCountry}";
            }

            yield return $"{address.Logradouro}, {cityStateCountry}";
        }

        if (!string.IsNullOrWhiteSpace(address.Bairro))
        {
            yield return $"{address.Bairro}, {cityStateCountry}";
        }

        yield return $"{FormatPostalCode(postalCode)}, {cityStateCountry}";
    }

    private static bool LooksLikeAddressMatch(string? displayName, PostalCodeAddress address)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return false;
        }

        var normalizedDisplayName = NormalizeSearchText(displayName);
        var normalizedCity = NormalizeSearchText(address.Localidade);
        var normalizedState = NormalizeSearchText(address.Uf);

        return !string.IsNullOrWhiteSpace(normalizedCity) &&
               normalizedDisplayName.Contains(normalizedCity, StringComparison.OrdinalIgnoreCase) &&
               (string.IsNullOrWhiteSpace(normalizedState) ||
                normalizedDisplayName.Contains(normalizedState, StringComparison.OrdinalIgnoreCase) ||
                normalizedDisplayName.Contains(NormalizeSearchText(address.Estado), StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeSearchText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    private decimal EstimateFallbackDistanceKm(string originPostalCode, string destinationPostalCode)
    {
        var originNumber = int.Parse(originPostalCode[..5], CultureInfo.InvariantCulture);
        var destinationNumber = int.Parse(destinationPostalCode[..5], CultureInfo.InvariantCulture);
        var cepSpread = Math.Abs(originNumber - destinationNumber) / 10000m;
        var fallbackDistance = Math.Clamp(GetMinimumDistanceKm() + (cepSpread * 8m), GetMinimumDistanceKm(), GetMaximumDistanceKm());
        return decimal.Round(fallbackDistance, 2);
    }

    private decimal GetRouteFactor()
    {
        return Math.Clamp(_options.Approximate.RouteFactor, 1.05m, 2.5m);
    }

    private decimal GetMinimumDistanceKm()
    {
        return Math.Clamp(_options.Approximate.MinimumDistanceKm, 0.2m, 5m);
    }

    private decimal GetMaximumDistanceKm()
    {
        return Math.Clamp(_options.Approximate.MaximumDistanceKm, 1m, 250m);
    }

    private string ResolveUserAgent()
    {
        return string.IsNullOrWhiteSpace(_options.Approximate.UserAgent)
            ? "ZeroPaper/1.0 (contact@zeropaper.local)"
            : _options.Approximate.UserAgent.Trim();
    }

    private static string FormatPostalCode(string postalCode)
    {
        return postalCode.Length == 8
            ? $"{postalCode[..5]}-{postalCode[5..]}"
            : postalCode;
    }

    private static double ParseCoordinate(string? value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var coordinate))
        {
            throw new InvalidOperationException("Coordenada invalida para o CEP informado.");
        }

        return coordinate;
    }

    private static decimal CalculateHaversineDistanceKm(
        double originLatitude,
        double originLongitude,
        double destinationLatitude,
        double destinationLongitude)
    {
        const double earthRadiusKm = 6371d;
        var latitudeDelta = DegreesToRadians(destinationLatitude - originLatitude);
        var longitudeDelta = DegreesToRadians(destinationLongitude - originLongitude);
        var originLatitudeRad = DegreesToRadians(originLatitude);
        var destinationLatitudeRad = DegreesToRadians(destinationLatitude);

        var a = Math.Sin(latitudeDelta / 2) * Math.Sin(latitudeDelta / 2) +
                Math.Cos(originLatitudeRad) * Math.Cos(destinationLatitudeRad) *
                Math.Sin(longitudeDelta / 2) * Math.Sin(longitudeDelta / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return decimal.Round((decimal)(earthRadiusKm * c), 4);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }

    private sealed class PostalCodeAddress
    {
        public string? Logradouro { get; set; }
        public string? Bairro { get; set; }
        public string? Localidade { get; set; }
        public string? Uf { get; set; }
        public string? Estado { get; set; }
        public bool Erro { get; set; }
    }
}
