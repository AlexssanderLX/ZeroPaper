using Microsoft.Extensions.Options;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class MockDeliveryDistanceProvider : IDeliveryDistanceProvider
{
    private readonly DeliveryDistanceOptions _options;

    public MockDeliveryDistanceProvider(IOptions<DeliveryDistanceOptions> options)
    {
        _options = options.Value;
    }

    public string Name => "mock";
    public bool IsConfigured => true;
    public bool IsTestMode => true;

    public Task<DeliveryDistanceResult> CalculateDistanceAsync(
        string originPostalCode,
        string destinationPostalCode,
        CancellationToken cancellationToken = default)
    {
        var originNumber = int.Parse(originPostalCode[..5]);
        var destinationNumber = int.Parse(destinationPostalCode[..5]);
        var cepSpread = Math.Abs(originNumber - destinationNumber) / 1000m;
        var baseDistance = _options.MockDistanceKm > 0 ? _options.MockDistanceKm : 4.2m;
        var distanceKm = decimal.Round(Math.Clamp(baseDistance + cepSpread, 0.8m, 35m), 2);

        return Task.FromResult(new DeliveryDistanceResult(distanceKm, Name, IsTestMode));
    }
}
