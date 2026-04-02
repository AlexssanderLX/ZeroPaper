using Microsoft.AspNetCore.Mvc;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/print-agent")]
public class PrintAgentController : ControllerBase
{
    private readonly IPrintAutomationService _printAutomationService;

    public PrintAgentController(IPrintAutomationService printAutomationService)
    {
        _printAutomationService = printAutomationService;
    }

    [HttpPost("heartbeat")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> HeartbeatAsync([FromBody] PrintAgentHeartbeatRequestDto request, CancellationToken cancellationToken)
    {
        await _printAutomationService.RegisterAgentHeartbeatAsync(GetAgentKey(), request, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/claim-next")]
    [ProducesResponseType(typeof(PrintAgentOrderJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClaimNextAsync([FromBody] PrintAgentClaimRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _printAutomationService.ClaimNextOrderJobAsync(GetAgentKey(), request, cancellationToken);
        return response is null ? NoContent() : Ok(response);
    }

    [HttpPost("orders/{orderId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteAsync(Guid orderId, [FromBody] CompletePrintJobRequestDto request, CancellationToken cancellationToken)
    {
        await _printAutomationService.CompleteOrderJobAsync(GetAgentKey(), orderId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/{orderId:guid}/fail")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> FailAsync(Guid orderId, [FromBody] FailPrintJobRequestDto request, CancellationToken cancellationToken)
    {
        await _printAutomationService.FailOrderJobAsync(GetAgentKey(), orderId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/complete-batch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteBatchAsync([FromBody] CompletePrintJobBatchRequestDto request, CancellationToken cancellationToken)
    {
        await _printAutomationService.CompleteOrderBatchAsync(GetAgentKey(), request, cancellationToken);
        return NoContent();
    }

    [HttpPost("orders/fail-batch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> FailBatchAsync([FromBody] FailPrintJobBatchRequestDto request, CancellationToken cancellationToken)
    {
        await _printAutomationService.FailOrderBatchAsync(GetAgentKey(), request, cancellationToken);
        return NoContent();
    }

    private string GetAgentKey()
    {
        var header = Request.Headers["X-ZP-Agent-Key"].ToString();
        ArgumentException.ThrowIfNullOrWhiteSpace(header);
        return header;
    }
}
