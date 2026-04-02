using System.Net;
using System.Net.Http.Json;

namespace ZeroPaper.PrintAgent;

internal sealed class PrintAgentApiClient
{
    private readonly HttpClient _httpClient = new();

    public async Task<PrintOrderJob?> ClaimNextAsync(AgentConfig config, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Post, config, "/api/print-agent/orders/claim-next");
        request.Content = JsonContent.Create(new
        {
            agentName = config.AgentName,
            printerName = config.PrinterName
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PrintOrderJob>(cancellationToken: cancellationToken);
    }

    public async Task CompleteAsync(AgentConfig config, Guid orderId, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Post, config, $"/api/print-agent/orders/{orderId}/complete");
        request.Content = JsonContent.Create(new
        {
            agentName = config.AgentName,
            printerName = config.PrinterName
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task CompleteBatchAsync(AgentConfig config, IReadOnlyList<Guid> orderIds, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Post, config, "/api/print-agent/orders/complete-batch");
        request.Content = JsonContent.Create(new
        {
            agentName = config.AgentName,
            printerName = config.PrinterName,
            orderIds
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task FailAsync(AgentConfig config, Guid orderId, string errorMessage, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Post, config, $"/api/print-agent/orders/{orderId}/fail");
        request.Content = JsonContent.Create(new
        {
            agentName = config.AgentName,
            printerName = config.PrinterName,
            errorMessage
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task FailBatchAsync(AgentConfig config, IReadOnlyList<Guid> orderIds, string errorMessage, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(HttpMethod.Post, config, "/api/print-agent/orders/fail-batch");
        request.Content = JsonContent.Create(new
        {
            agentName = config.AgentName,
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
        request.Content = JsonContent.Create(new
        {
            agentName = config.AgentName,
            printerName = config.PrinterName,
            appVersion = typeof(PrintAgentApiClient).Assembly.GetName().Version?.ToString()
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

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
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
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
}
