using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IDeliveryDistanceProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    bool IsTestMode { get; }
    Task<DeliveryDistanceResult> CalculateDistanceAsync(
        string originPostalCode,
        string destinationPostalCode,
        CancellationToken cancellationToken = default);
}
