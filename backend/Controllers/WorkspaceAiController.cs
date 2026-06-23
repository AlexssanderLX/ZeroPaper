using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/workspace/ai")]
public class WorkspaceAiController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IAiAssistantService _aiAssistantService;
    private readonly IWhatsAppIntegrationService _whatsAppIntegrationService;

    public WorkspaceAiController(
        IAuthSessionService authSessionService,
        IAiAssistantService aiAssistantService,
        IWhatsAppIntegrationService whatsAppIntegrationService)
    {
        _authSessionService = authSessionService;
        _aiAssistantService = aiAssistantService;
        _whatsAppIntegrationService = whatsAppIntegrationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AiAssistantSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureAiModuleEnabled(session);
        return accessResult ?? Ok(await _aiAssistantService.GetSettingsAsync(session, cancellationToken));
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(AiAssistantQuickStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuickStatusAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureAiModuleEnabled(session);
        return accessResult ?? Ok(await _aiAssistantService.GetQuickStatusAsync(session, cancellationToken));
    }

    [HttpPatch("status")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(AiAssistantQuickStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateQuickStatusAsync(
        [FromBody] UpdateAiAssistantQuickStatusRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureAiModuleEnabled(session);
        return accessResult ?? Ok(await _aiAssistantService.UpdateQuickStatusAsync(session, request.IsEnabled, cancellationToken));
    }

    [HttpPatch]
    [ProducesResponseType(typeof(AiAssistantSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettingsAsync([FromBody] UpdateAiAssistantSettingsRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureAiModuleEnabled(session);
        if (accessResult is not null)
        {
            return accessResult;
        }

        try
        {
            return Ok(await _aiAssistantService.UpdateSettingsAsync(session, request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Configuracao invalida",
                Detail = exception.Message
            });
        }
    }

    [HttpPost("generate-template")]
    [ProducesResponseType(typeof(AiAssistantSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateTemplateAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureAiModuleEnabled(session);
        return accessResult ?? Ok(await _aiAssistantService.GenerateTemplateAsync(session, cancellationToken));
    }

    [HttpPost("test")]
    [ProducesResponseType(typeof(AiAssistantTestResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestAssistantAsync([FromBody] AiAssistantTestRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureAiModuleEnabled(session);
        if (accessResult is not null)
        {
            return accessResult;
        }

        try
        {
            return Ok(await _aiAssistantService.TestAssistantAsync(session, request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Mensagem invalida",
                Detail = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "IA indisponivel",
                Detail = exception.Message
            });
        }
    }

    [HttpPost("whatsapp/prepare")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(WhatsAppConnectionSnapshotDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PrepareWhatsAppConnectionAsync([FromBody] PrepareWhatsAppConnectionRequestDto? request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        var accessResult = EnsureAiModuleEnabled(session);
        if (accessResult is not null)
        {
            return accessResult;
        }

        try
        {
            return Ok(await _whatsAppIntegrationService.PrepareEvolutionConnectionAsync(
                session.CompanyId,
                request?.PhoneNumber,
                request?.ForceNewSession ?? false,
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Configuracao invalida",
                Detail = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "WhatsApp indisponivel",
                Detail = exception.Message
            });
        }
    }

    private async Task<WorkspaceSessionContext?> GetRequiredSessionAsync(CancellationToken cancellationToken)
    {
        return await _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
    }

    private ObjectResult? EnsureAiModuleEnabled(WorkspaceSessionContext session)
    {
        return session.IncludesAiAssistantModule
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Modulo indisponivel",
                Detail = "A IA nao faz parte do plano atual da unidade."
            });
    }
}
