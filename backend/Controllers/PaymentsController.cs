using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/workspace/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly PublicAppOptions _publicAppOptions;

    public PaymentsController(
        IAuthSessionService authSessionService,
        IMercadoPagoService mercadoPagoService,
        IOptions<PublicAppOptions> publicAppOptions)
    {
        _authSessionService = authSessionService;
        _mercadoPagoService = mercadoPagoService;
        _publicAppOptions = publicAppOptions.Value;
    }

    [HttpGet("mercadopago/status")]
    [ProducesResponseType(typeof(MercadoPagoStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _mercadoPagoService.GetStatusAsync(session, cancellationToken));
    }

    [HttpPost("mercadopago/connect")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(MercadoPagoConnectResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartConnectionAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _mercadoPagoService.StartConnectionAsync(session, cancellationToken));
    }

    [HttpDelete("mercadopago/disconnect")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DisconnectAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        await _mercadoPagoService.DisconnectAsync(session, cancellationToken);
        return NoContent();
    }

    [HttpGet("mercadopago/callback")]
    public async Task<IActionResult> HandleCallbackAsync(
        [FromQuery] string? code,
        [FromQuery] string? state,
        CancellationToken cancellationToken)
    {
        var connected = await _mercadoPagoService.HandleOAuthCallbackAsync(state, code, cancellationToken);
        var status = connected ? "connected" : "error";
        return Redirect($"{ResolveFrontendBaseUrl()}/app/pagamentos?mp={status}");
    }

    [HttpPost("~/api/public/tables/{publicCode}/orders/{orderId:guid}/mercadopago/checkout")]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(typeof(MercadoPagoCheckoutResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePublicCheckoutAsync(
        string publicCode,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return Ok(await _mercadoPagoService.CreatePublicCheckoutAsync(publicCode, orderId, cancellationToken));
    }

    [HttpPost("~/api/public/payments/mercadopago/webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> HandleWebhookAsync(CancellationToken cancellationToken)
    {
        var paymentId = ResolvePaymentIdFromQuery();
        var orderId = ResolveOrderIdFromQuery();

        if (string.IsNullOrWhiteSpace(paymentId) && Request.ContentLength.GetValueOrDefault() > 0)
        {
            try
            {
                using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
                paymentId = ResolvePaymentIdFromBody(document.RootElement);
            }
            catch (JsonException)
            {
                paymentId = null;
            }
        }

        var processed = await _mercadoPagoService.HandlePaymentNotificationAsync(paymentId, orderId, cancellationToken);
        return Ok(new { processed });
    }

    private async Task<WorkspaceSessionContext?> GetRequiredSessionAsync(CancellationToken cancellationToken)
    {
        return await _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
    }

    private string ResolveFrontendBaseUrl()
    {
        var configured = Environment.GetEnvironmentVariable("PUBLIC_APP_BASE_URL")
            ?? _publicAppOptions.BaseUrl;

        return string.IsNullOrWhiteSpace(configured)
            ? string.Empty
            : configured.Trim().TrimEnd('/');
    }

    private string? ResolvePaymentIdFromQuery()
    {
        var id = Request.Query["id"].FirstOrDefault()
            ?? Request.Query["data.id"].FirstOrDefault()
            ?? Request.Query["payment_id"].FirstOrDefault();

        var topic = Request.Query["topic"].FirstOrDefault()
            ?? Request.Query["type"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(topic) &&
            !topic.Contains("payment", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return id;
    }

    private Guid? ResolveOrderIdFromQuery()
    {
        var value = Request.Query["orderId"].FirstOrDefault();
        return Guid.TryParse(value, out var orderId) ? orderId : null;
    }

    private static string? ResolvePaymentIdFromBody(JsonElement root)
    {
        if (root.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("id", out var dataId))
        {
            return dataId.ToString();
        }

        if (root.TryGetProperty("id", out var id))
        {
            return id.ToString();
        }

        return null;
    }
}
