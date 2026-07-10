using Microsoft.AspNetCore.Mvc;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/workspace/sellers")]
public class WorkspaceSellersController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly ISalesAgentService _salesAgentService;
    private readonly IWorkspaceService _workspaceService;

    public WorkspaceSellersController(
        IAuthSessionService authSessionService,
        ISalesAgentService salesAgentService,
        IWorkspaceService workspaceService)
    {
        _authSessionService = authSessionService;
        _salesAgentService = salesAgentService;
        _workspaceService = workspaceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SalesAgentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null) return Unauthorized();

        var accessResult = EnsureSalesAgentsEnabled(session);
        if (accessResult is not null) return accessResult;

        return Ok(await _salesAgentService.GetByCompanyAsync(session, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(SalesAgentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateSalesAgentRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null) return Unauthorized();

        var accessResult = EnsureSalesAgentsEnabled(session);
        if (accessResult is not null) return accessResult;

        var result = await _salesAgentService.CreateAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{agentId:guid}")]
    [ProducesResponseType(typeof(SalesAgentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(Guid agentId, [FromBody] UpdateSalesAgentRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null) return Unauthorized();

        var accessResult = EnsureSalesAgentsEnabled(session);
        if (accessResult is not null) return accessResult;

        try
        {
            return Ok(await _salesAgentService.UpdateAsync(session, agentId, request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPatch("{agentId:guid}/status")]
    [ProducesResponseType(typeof(SalesAgentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatusAsync(Guid agentId, [FromBody] UpdateSalesAgentStatusRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null) return Unauthorized();

        var accessResult = EnsureSalesAgentsEnabled(session);
        if (accessResult is not null) return accessResult;

        try
        {
            return Ok(await _salesAgentService.UpdateStatusAsync(session, agentId, request.IsActive, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{agentId:guid}/orders")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrdersAsync(Guid agentId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null) return Unauthorized();

        var accessResult = EnsureSalesAgentsEnabled(session);
        if (accessResult is not null) return accessResult;

        return Ok(await _workspaceService.GetOrdersByAgentAsync(session, agentId, cancellationToken));
    }

    private async Task<Services.Models.WorkspaceSessionContext?> GetRequiredSessionAsync(CancellationToken cancellationToken)
        => await _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);

    private ObjectResult? EnsureSalesAgentsEnabled(Services.Models.WorkspaceSessionContext session)
        => session.HasSalesAgents
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Modulo indisponivel",
                Detail = "Vendedores nao fazem parte do plano atual da unidade."
            });
}
