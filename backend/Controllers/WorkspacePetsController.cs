using Microsoft.AspNetCore.Mvc;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/workspace/pets")]
public sealed class WorkspacePetsController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IPetService _petService;

    public WorkspacePetsController(IAuthSessionService authSessionService, IPetService petService)
    {
        _authSessionService = authSessionService;
        _petService = petService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] Guid? customerProfileId, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        var denied = EnsureAccess(session);
        return denied ?? Ok(await _petService.GetAsync(session!, customerProfileId, cancellationToken));
    }

    [HttpGet("{petId:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid petId, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        var denied = EnsureAccess(session);
        return denied ?? Ok(await _petService.GetByIdAsync(session!, petId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreatePetRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        var denied = EnsureAccess(session);
        if (denied is not null) return denied;

        var created = await _petService.CreateAsync(session!, request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { petId = created.Id }, created);
    }

    [HttpPut("{petId:guid}")]
    public async Task<IActionResult> UpdateAsync(Guid petId, [FromBody] UpdatePetRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        var denied = EnsureAccess(session);
        return denied ?? Ok(await _petService.UpdateAsync(session!, petId, request, cancellationToken));
    }

    [HttpPatch("{petId:guid}/status")]
    public async Task<IActionResult> UpdateStatusAsync(Guid petId, [FromBody] UpdatePetStatusRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        var denied = EnsureAccess(session);
        return denied ?? Ok(await _petService.UpdateStatusAsync(session!, petId, request.IsActive, cancellationToken));
    }

    private Task<WorkspaceSessionContext?> GetSessionAsync(CancellationToken cancellationToken) =>
        _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);

    private ObjectResult? EnsureAccess(WorkspaceSessionContext? session)
    {
        if (session is null) return UnauthorizedProblem();
        return session.BusinessSegment == BusinessSegment.PetShop
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Title = "Modulo indisponivel", Detail = "Animais nao estao disponiveis para esta empresa.", Status = 403 });
    }

    private ObjectResult UnauthorizedProblem() => StatusCode(StatusCodes.Status401Unauthorized,
        new ProblemDetails { Title = "Sessao invalida", Status = 401 });
}
