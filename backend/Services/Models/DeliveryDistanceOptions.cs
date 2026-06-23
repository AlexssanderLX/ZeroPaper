namespace ZeroPaper.Services.Models;

public class DeliveryDistanceOptions
{
    public const string SectionName = "Delivery";
    public const string DefaultProvider = "Approximate";

    public string? DistanceProvider { get; set; }
    public int CacheDays { get; set; } = 30;
    public decimal MockDistanceKm { get; set; } = 4.2m;
    public ApproximateDeliveryOptions Approximate { get; set; } = new();
    public GoogleMapsDeliveryOptions GoogleMaps { get; set; } = new();
}

public class ApproximateDeliveryOptions
{
    public string? BaseUrl { get; set; }
    public decimal RouteFactor { get; set; } = 1.45m;
    public decimal MinimumDistanceKm { get; set; } = 0.5m;
    public decimal MaximumDistanceKm { get; set; } = 35m;
    public string? UserAgent { get; set; }
}

public class GoogleMapsDeliveryOptions
{
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
}
