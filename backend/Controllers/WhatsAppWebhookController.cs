using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IWhatsAppIntegrationService _whatsAppIntegrationService;

    public WhatsAppWebhookController(IWhatsAppIntegrationService whatsAppIntegrationService)
    {
        _whatsAppIntegrationService = whatsAppIntegrationService;
    }

    [HttpPost("api/integrations/whatsapp/evolution/{instanceId}/events")]
    [EnableRateLimiting("webhook-ingress")]
    public async Task<IActionResult> EvolutionEventsAsync(string instanceId, [FromQuery] string? key, [FromBody] JsonDocument payload, CancellationToken cancellationToken)
    {
        await _whatsAppIntegrationService.HandleEvolutionWebhookAsync(instanceId, key, payload, cancellationToken);
        return NoContent();
    }

    [HttpPost("api/integrations/whatsapp/zapi/{instanceId}/receive")]
    [EnableRateLimiting("integration-write")]
    public async Task<IActionResult> ReceiveAsync(string instanceId, [FromQuery] string? key, [FromBody] JsonDocument payload, CancellationToken cancellationToken)
    {
        await _whatsAppIntegrationService.HandleReceiveWebhookAsync(instanceId, key, payload, cancellationToken);
        return NoContent();
    }

    [HttpPost("api/integrations/whatsapp/zapi/{instanceId}/message-status")]
    [EnableRateLimiting("integration-write")]
    public async Task<IActionResult> MessageStatusAsync(string instanceId, [FromQuery] string? key, [FromBody] JsonDocument payload, CancellationToken cancellationToken)
    {
        await _whatsAppIntegrationService.HandleMessageStatusWebhookAsync(instanceId, key, payload, cancellationToken);
        return NoContent();
    }

    [HttpPost("api/integrations/whatsapp/zapi/{instanceId}/connected")]
    [EnableRateLimiting("integration-write")]
    public async Task<IActionResult> ConnectedAsync(string instanceId, [FromQuery] string? key, [FromBody] JsonDocument payload, CancellationToken cancellationToken)
    {
        await _whatsAppIntegrationService.HandleConnectedWebhookAsync(instanceId, key, payload, cancellationToken);
        return NoContent();
    }

    [HttpPost("api/integrations/whatsapp/zapi/{instanceId}/disconnected")]
    [EnableRateLimiting("integration-write")]
    public async Task<IActionResult> DisconnectedAsync(string instanceId, [FromQuery] string? key, [FromBody] JsonDocument payload, CancellationToken cancellationToken)
    {
        await _whatsAppIntegrationService.HandleDisconnectedWebhookAsync(instanceId, key, payload, cancellationToken);
        return NoContent();
    }
}
