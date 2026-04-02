using Microsoft.AspNetCore.Mvc;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/workspace/printing")]
public class WorkspacePrintingController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IPrintAutomationService _printAutomationService;

    public WorkspacePrintingController(IAuthSessionService authSessionService, IPrintAutomationService printAutomationService)
    {
        _authSessionService = authSessionService;
        _printAutomationService = printAutomationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PrintingSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrintingAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _printAutomationService.GetPrintingSettingsAsync(session, cancellationToken));
    }

    [HttpPatch]
    [ProducesResponseType(typeof(PrintingSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePrintingAsync([FromBody] UpdatePrintingSettingsRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _printAutomationService.UpdatePrintingSettingsAsync(session, request, cancellationToken));
    }

    [HttpPost("agent-key")]
    [ProducesResponseType(typeof(RotatePrintingAgentKeyResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RotateAgentKeyAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _printAutomationService.RotatePrintingAgentKeyAsync(session, cancellationToken));
    }

    [HttpPost("orders/{orderId:guid}/requeue")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RequeueOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        await _printAutomationService.RequeueOrderPrintAsync(session, orderId, cancellationToken);
        return NoContent();
    }

    private async Task<WorkspaceSessionContext?> GetRequiredSessionAsync(CancellationToken cancellationToken)
    {
        return await _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
    }
}
