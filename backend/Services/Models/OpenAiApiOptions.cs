namespace ZeroPaper.Services.Models;

public class OpenAiApiOptions
{
    public const string SectionName = "OpenAI";
    public const string DefaultBaseUrl = "https://api.openai.com/v1/";
    public const string DefaultModel = "gpt-5.4-mini";

    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public string? DefaultModelName { get; set; }
}
