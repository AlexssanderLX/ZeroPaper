using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsurePrintingModuleEnabled(session);
        return accessResult ?? Ok(await _printAutomationService.GetPrintingSettingsAsync(session, cancellationToken));
    }

    [HttpPatch]
    [ProducesResponseType(typeof(PrintingSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePrintingAsync([FromBody] UpdatePrintingSettingsRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsurePrintingModuleEnabled(session);
        return accessResult ?? Ok(await _printAutomationService.UpdatePrintingSettingsAsync(session, request, cancellationToken));
    }

    [HttpPost("agent-key")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(RotatePrintingAgentKeyResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RotateAgentKeyAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsurePrintingModuleEnabled(session);
        var autoPrintResult = accessResult ?? EnsureAutoPrintEnabled(session);
        return autoPrintResult ?? Ok(await _printAutomationService.RotatePrintingAgentKeyAsync(session, cancellationToken));
    }

    [HttpPost("agent-token")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(RotatePrintingAgentKeyResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RotateAgentTokenAsync(CancellationToken cancellationToken)
    {
        return await RotateAgentKeyAsync(cancellationToken);
    }

    [HttpPost("test-job")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(PrintTestJobResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateTestJobAsync([FromBody] CreatePrintTestJobRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsurePrintingModuleEnabled(session);
        var autoPrintResult = accessResult ?? EnsureAutoPrintEnabled(session);
        return autoPrintResult ?? Ok(await _printAutomationService.CreateTestJobAsync(session, request, cancellationToken));
    }

    [HttpPost("orders/{orderId:guid}/requeue")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RequeueOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsurePrintingModuleEnabled(session);
        if (accessResult is not null)
        {
            return accessResult;
        }

        await _printAutomationService.RequeueOrderPrintAsync(session, orderId, cancellationToken);
        return NoContent();
    }

    private async Task<WorkspaceSessionContext?> GetRequiredSessionAsync(CancellationToken cancellationToken)
    {
        return await _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
    }

    private ObjectResult? EnsurePrintingModuleEnabled(WorkspaceSessionContext session)
    {
        return session.IncludesPrintingModule
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Modulo indisponivel",
                Detail = "A impressao nao faz parte do plano atual da unidade."
            });
    }

    private ObjectResult? EnsureAutoPrintEnabled(WorkspaceSessionContext session)
    {
        return session.HasAutoPrint
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Recurso indisponivel",
                Detail = "A impressao automatica nao faz parte do plano atual da unidade."
            });
    }
}
