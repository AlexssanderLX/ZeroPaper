using Microsoft.AspNetCore.Mvc;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/workspace/appointments")]
public sealed class WorkspaceAppointmentsController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IAppointmentService _appointmentService;

    public WorkspaceAppointmentsController(IAuthSessionService authSessionService, IAppointmentService appointmentService)
    {
        _authSessionService = authSessionService;
        _appointmentService = appointmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        var denied = EnsureAccess(session);
        return denied ?? Ok(await _appointmentService.GetAsync(session!, fromUtc, toUtc, cancellationToken));
    }

    [HttpGet("{appointmentId:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        var denied = EnsureAccess(session);
        return denied ?? Ok(await _appointmentService.GetByIdAsync(session!, appointmentId, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateAppointmentRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        var denied = EnsureAccess(session);
        if (denied is not null) return denied;
        var created = await _appointmentService.CreateAsync(session!, request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { appointmentId = created.Id }, created);
    }

    [HttpPut("{appointmentId:guid}/schedule")]
    public async Task<IActionResult> RescheduleAsync(Guid appointmentId, [FromBody] RescheduleAppointmentRequestDto request, CancellationToken cancellationToken)
        => await ExecuteAsync(session => _appointmentService.RescheduleAsync(session, appointmentId, request, cancellationToken), cancellationToken);

    [HttpPut("{appointmentId:guid}/notes")]
    public async Task<IActionResult> UpdateNotesAsync(Guid appointmentId, [FromBody] UpdateAppointmentNotesRequestDto request, CancellationToken cancellationToken)
        => await ExecuteAsync(session => _appointmentService.UpdateNotesAsync(session, appointmentId, request, cancellationToken), cancellationToken);

    [HttpPut("{appointmentId:guid}/assignee")]
    public async Task<IActionResult> AssignAsync(Guid appointmentId, [FromBody] AssignAppointmentRequestDto request, CancellationToken cancellationToken)
        => await ExecuteAsync(session => _appointmentService.AssignAsync(session, appointmentId, request.AssignedUserId, cancellationToken), cancellationToken);

    [HttpPut("{appointmentId:guid}/order")]
    public async Task<IActionResult> LinkOrderAsync(Guid appointmentId, [FromBody] LinkAppointmentOrderRequestDto request, CancellationToken cancellationToken)
        => await ExecuteAsync(session => _appointmentService.LinkOrderAsync(session, appointmentId, request.CustomerOrderId, cancellationToken), cancellationToken);

    [HttpPatch("{appointmentId:guid}/status")]
    public async Task<IActionResult> ChangeStatusAsync(Guid appointmentId, [FromBody] ChangeAppointmentStatusRequestDto request, CancellationToken cancellationToken)
        => await ExecuteAsync(session => _appointmentService.ChangeStatusAsync(session, appointmentId, request, cancellationToken), cancellationToken);

    private async Task<IActionResult> ExecuteAsync(Func<WorkspaceSessionContext, Task<AppointmentDto>> action, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        var denied = EnsureAccess(session);
        return denied ?? Ok(await action(session!));
    }

    private Task<WorkspaceSessionContext?> GetSessionAsync(CancellationToken cancellationToken) =>
        _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);

    private ObjectResult? EnsureAccess(WorkspaceSessionContext? session)
    {
        if (session is null)
            return StatusCode(StatusCodes.Status401Unauthorized, new ProblemDetails { Title = "Sessao invalida", Status = 401 });

        return session.BusinessSegment == BusinessSegment.PetShop
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Title = "Modulo indisponivel", Detail = "Agenda nao esta disponivel para esta empresa.", Status = 403 });
    }
}
