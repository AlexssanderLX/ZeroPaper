using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroPaper.PrintAgent;

internal sealed class PrintAgentApiClient
{
    private const string AgentBuildVersion = "2026.05.03-compat-paper";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient = new();

    public async Task<PrintOrderJob?> ClaimNextAsync(AgentConfig config, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Post, config, "/api/print-agent/orders/claim-next");
        request.Content = CreateJsonContent(new
        {
            agentName = BuildAgentName(config),
            printerName = config.PrinterName
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonAsync<PrintOrderJob>(response, cancellationToken);
    }

    public async Task CompleteAsync(AgentConfig config, Guid orderId, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Post, config, $"/api/print-agent/orders/{orderId}/complete");
        request.Content = CreateJsonContent(new
        {
            agentName = BuildAgentName(config),
            printerName = config.PrinterName
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task CompleteBatchAsync(AgentConfig config, IReadOnlyList<Guid> orderIds, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Post, config, "/api/print-agent/orders/complete-batch");
        request.Content = CreateJsonContent(new
        {
            agentName = BuildAgentName(config),
            printerName = config.PrinterName,
            orderIds
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task FailAsync(AgentConfig config, Guid orderId, string errorMessage, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Post, config, $"/api/print-agent/orders/{orderId}/fail");
        request.Content = CreateJsonContent(new
        {
            agentName = BuildAgentName(config),
            printerName = config.PrinterName,
            errorMessage
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task FailBatchAsync(AgentConfig config, IReadOnlyList<Guid> orderIds, string errorMessage, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Post, config, "/api/print-agent/orders/fail-batch");
        request.Content = CreateJsonContent(new
        {
            agentName = BuildAgentName(config),
            printerName = config.PrinterName,
            errorMessage,
            orderIds
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task HeartbeatAsync(AgentConfig config, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Post, config, "/api/print-agent/heartbeat");
        request.Content = CreateJsonContent(new
        {
            agentName = BuildAgentName(config),
            printerName = config.PrinterName,
            appVersion = AgentBuildVersion
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, AgentConfig config, string relativePath)
    {
        var baseUrl = config.ApiBaseUrl.Trim().TrimEnd('/');

        if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = "https://" + baseUrl;
        }

        var request = new HttpRequestMessage(method, $"{baseUrl}{relativePath}");
        request.Headers.Add("X-ZP-Agent-Key", config.AgentKey.Trim());
        return request;
    }

    private static string BuildAgentName(AgentConfig config)
    {
        var baseName = string.IsNullOrWhiteSpace(config.AgentName)
            ? Environment.MachineName
            : config.AgentName.Trim();

        return $"{baseName} / {AgentBuildVersion}";
    }

    private static StringContent CreateJsonContent(object payload)
    {
        return new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
    }

    private static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
#if NETFRAMEWORK
        var body = await response.Content.ReadAsStringAsync();
#else
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
#endif
        cancellationToken.ThrowIfCancellationRequested();
        return string.IsNullOrWhiteSpace(body)
            ? default
            : JsonSerializer.Deserialize<T>(body, JsonOptions);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

#if NETFRAMEWORK
        var body = await response.Content.ReadAsStringAsync();
#else
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
#endif
        var detail = string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body;
        throw new InvalidOperationException(detail ?? "Falha na comunicacao com o ZeroPaper.");
    }
}

internal sealed class PrintOrderJob
{
    public Guid OrderId { get; set; }
    public int Number { get; set; }
    public string PaperProfile { get; set; } = "Thermal80mm";
    public int OrdersPerPage { get; set; } = 1;
    public string RestaurantName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? DeliveryPhone { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryNumber { get; set; }
    public string? DeliveryComplement { get; set; }
    public string? DeliveryPostalCode { get; set; }
    public decimal DeliveryFreightAmount { get; set; }
    public decimal? DeliveryDistanceKm { get; set; }
    public string? Notes { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PrintOrderItem> Items { get; set; } = [];
}

internal sealed class PrintOrderItem
{
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal Quantity { get; set; }
    public decimal BaseUnitPrice { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public List<PrintOrderAdditional> Additionals { get; set; } = [];
}

internal sealed class PrintOrderAdditional
{
    public string GroupName { get; set; } = string.Empty;
    public string OptionName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}
