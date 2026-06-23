using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/print-agent")]
[EnableRateLimiting("integration-write")]
public class PrintAgentController : ControllerBase
{
    private readonly IPrintAutomationService _printAutomationService;

    public PrintAgentController(IPrintAutomationService printAutomationService)
    {
        _printAutomationService = printAutomationService;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(PrintAgentRegistrationResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegisterAsync([FromBody] PrintAgentHeartbeatRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _printAutomationService.RegisterAgentAsync(GetAgentToken(), request, cancellationToken));
    }

    [HttpPost("heartbeat")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> HeartbeatAsync([FromBody] PrintAgentHeartbeatRequestDto request, CancellationToken cancellationToken)
    {
        await _printAutomationService.RegisterAgentHeartbeatAsync(GetAgentToken(), request, cancellationToken);
        return NoContent();
    }

    [HttpPost("jobs/claim-next")]
    [ProducesResponseType(typeof(PrintAgentOrderJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClaimNextJobAsync([FromBody] PrintAgentClaimRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _printAutomationService.ClaimNextOrderJobAsync(GetAgentToken(), request, cancellationToken);
        return response is null ? NoContent() : Ok(response);
    }

    [HttpPost("orders/claim-next")]
    [ProducesResponseType(typeof(PrintAgentOrderJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClaimNextAsync([FromBody] PrintAgentClaimRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _printAutomationService.ClaimNextOrderJobAsync(GetAgentToken(), request, cancellationToken);
        return response is null ? NoContent() : Ok(response);
    }

    [HttpPost("jobs/{jobId:guid}/printed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkPrintedAsync(Guid jobId, [FromBody] CompletePrintJobRequestDto request, CancellationToken cancellationToken)
    {
        await _printAutomationService.CompleteOrderJobAsync(GetAgentToken(), jobId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/{orderId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteAsync(Guid orderId, [FromBody] CompletePrintJobRequestDto request, CancellationToken cancellationToken)
    {
        await _printAutomationService.CompleteOrderJobAsync(GetAgentToken(), orderId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("jobs/{jobId:guid}/error")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkErrorAsync(Guid jobId, [FromBody] FailPrintJobRequestDto request, CancellationToken cancellationToken)
    {
        await _printAutomationService.FailOrderJobAsync(GetAgentToken(), jobId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/{orderId:guid}/fail")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> FailAsync(Guid orderId, [FromBody] FailPrintJobRequestDto request, CancellationToken cancellationToken)
    {
        await _printAutomationService.FailOrderJobAsync(GetAgentToken(), orderId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("jobs/printed-batch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkPrintedBatchAsync([FromBody] CompletePrintJobBatchRequestDto request, CancellationToken cancellationToken)
    {
        await _printAutomationService.CompleteOrderBatchAsync(GetAgentToken(), request, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/complete-batch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteBatchAsync([FromBody] CompletePrintJobBatchRequestDto request, CancellationToken cancellationToken)
    {
        await _printAutomationService.CompleteOrderBatchAsync(GetAgentToken(), request, cancellationToken);
        return NoContent();
    }

    [HttpPost("jobs/error-batch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkErrorBatchAsync([FromBody] FailPrintJobBatchRequestDto request, CancellationToken cancellationToken)
    {
        await _printAutomationService.FailOrderBatchAsync(GetAgentToken(), request, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/fail-batch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> FailBatchAsync([FromBody] FailPrintJobBatchRequestDto request, CancellationToken cancellationToken)
    {
        await _printAutomationService.FailOrderBatchAsync(GetAgentToken(), request, cancellationToken);
        return NoContent();
    }

    private string GetAgentToken()
    {
        var header = Request.Headers["X-ZP-Agent-Token"].ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            header = Request.Headers["X-ZP-Agent-Key"].ToString();
        }

        if (string.IsNullOrWhiteSpace(header) && Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            header = Request.Headers.Authorization.ToString()["Bearer ".Length..];
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(header);
        return header;
    }
}
