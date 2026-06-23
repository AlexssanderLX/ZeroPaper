namespace ZeroPaper.Services.Models;

public class EvolutionApiOptions
{
    public const string SectionName = "WhatsApp:Evolution";

    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? DefaultIntegration { get; set; }
}
