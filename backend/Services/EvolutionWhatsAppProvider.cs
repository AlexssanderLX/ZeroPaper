using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

internal sealed class EvolutionWhatsAppProvider : IWhatsAppProvider
{
    private readonly HttpClient _httpClient;
    private readonly EvolutionApiOptions _options;
    private readonly ILogger<EvolutionWhatsAppProvider> _logger;

    public EvolutionWhatsAppProvider(
        HttpClient httpClient,
        IOptions<EvolutionApiOptions> options,
        ILogger<EvolutionWhatsAppProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public WhatsAppProvider Provider => WhatsAppProvider.Evolution;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.BaseUrl) &&
        !string.IsNullOrWhiteSpace(ResolveRequiredApiKey());

    public async Task<WhatsAppProviderSendResult> SendTextAsync(
        Company company,
        string phone,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(company.WhatsAppInstanceId))
        {
            return WhatsAppProviderSendResult.Failed("MISSING_INSTANCE");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            BuildApiUrl($"message/sendText/{Uri.EscapeDataString(company.WhatsAppInstanceId)}"));
        request.Headers.TryAddWithoutValidation("apikey", ResolveRequiredApiKey());
        request.Content = JsonContent.Create(new
        {
            number = phone,
            text = message
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Falha ao enviar mensagem via Evolution para a unidade {CompanyId}. Status: {StatusCode}.",
                company.Id,
                (int)response.StatusCode);

            return WhatsAppProviderSendResult.Failed(((int)response.StatusCode).ToString());
        }

        return new WhatsAppProviderSendResult(
            true,
            ExtractExternalMessageId(responseText),
            "SENT");
    }

    private string? ResolveApiKey()
    {
        return Environment.GetEnvironmentVariable("WHATSAPP__EVOLUTION__APIKEY")
            ?? Environment.GetEnvironmentVariable("WHATSAPP__EVOLUTION__API_KEY")
            ?? _options.ApiKey;
    }

    private string ResolveRequiredApiKey()
    {
        return ResolveApiKey()
            ?? throw new InvalidOperationException("A Evolution Lite ainda nao tem API key configurada no servidor.");
    }

    private Uri BuildApiUrl(string relativePath)
    {
        var configuredBaseUrl = Environment.GetEnvironmentVariable("WHATSAPP__EVOLUTION__BASEURL")
            ?? Environment.GetEnvironmentVariable("WHATSAPP__EVOLUTION__BASE_URL")
            ?? _options.BaseUrl;
        if (string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            throw new InvalidOperationException("A Evolution Lite ainda nao tem base URL configurada no servidor.");
        }

        var normalizedBaseUrl = configuredBaseUrl.TrimEnd('/') + "/";
        return new Uri(new Uri(normalizedBaseUrl, UriKind.Absolute), relativePath);
    }

    private static string? ExtractExternalMessageId(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseText);
            return GetString(document.RootElement, ["key", "id"], ["message", "key", "id"], ["id"]);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement element, params string[][] paths)
    {
        foreach (var path in paths)
        {
            var current = element;
            var found = true;
            foreach (var segment in path)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                {
                    found = false;
                    break;
                }
            }

            if (found && current.ValueKind == JsonValueKind.String)
            {
                return current.GetString();
            }
        }

        return null;
    }
}
