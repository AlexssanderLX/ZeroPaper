using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ZeroPaper.Security;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Controllers;

[ApiController]
[Authorize(Policy = ZeroPaperSecurity.WorkspacePolicy)]
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
    public async Task<IActionResult> GetAsync([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, [FromQuery] AppointmentStatus? status,
        [FromQuery] Guid? petId, [FromQuery] Guid? customerId, [FromQuery] Guid? assignedUserId, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        var denied = EnsureAccess(session);
        return denied ?? Ok(await _appointmentService.GetAsync(session!, fromUtc, toUtc, status, petId, customerId, assignedUserId, cancellationToken));
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

    [HttpPost("{appointmentId:guid}/order")]
    public async Task<IActionResult> CreateOrderAsync(Guid appointmentId, [FromBody] CreateAppointmentOrderRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken); var denied = EnsureAccess(session);
        return denied ?? Ok(await _appointmentService.CreateOrderAsync(session!, appointmentId, request, cancellationToken));
    }

    [HttpPatch("{appointmentId:guid}/status")]
    public async Task<IActionResult> ChangeStatusAsync(Guid appointmentId, [FromBody] ChangeAppointmentStatusRequestDto request, CancellationToken cancellationToken)
        => await ExecuteAsync(session => _appointmentService.ChangeStatusAsync(session, appointmentId, request, cancellationToken), cancellationToken);

    [HttpGet("{appointmentId:guid}/history")]
    public async Task<IActionResult> GetHistoryAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken); var denied = EnsureAccess(session);
        return denied ?? Ok(await _appointmentService.GetHistoryAsync(session!, appointmentId, cancellationToken));
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken); var denied = EnsureAccess(session);
        return denied ?? Ok(await _appointmentService.GetSettingsAsync(session!, cancellationToken));
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettingsAsync([FromBody] UpdateAppointmentScheduleSettingsRequestDto request, CancellationToken cancellationToken)
        => await ExecuteSettingsAsync(session => _appointmentService.UpdateSettingsAsync(session, request, cancellationToken), cancellationToken);

    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailabilityAsync([FromQuery] DateOnly date, [FromQuery] Guid serviceId, [FromQuery] Guid? assignedUserId, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken); var denied = EnsureAccess(session);
        return denied ?? Ok(await _appointmentService.GetAvailabilityAsync(session!, date, serviceId, assignedUserId, cancellationToken));
    }

    [HttpGet("blocks")]
    public async Task<IActionResult> GetBlocksAsync([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken); var denied = EnsureAccess(session);
        return denied ?? Ok(await _appointmentService.GetBlocksAsync(session!, fromUtc, toUtc, cancellationToken));
    }

    [HttpPost("blocks")]
    public async Task<IActionResult> CreateBlockAsync([FromBody] CreateAppointmentBlockRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken); var denied = EnsureAccess(session);
        return denied ?? Ok(await _appointmentService.CreateBlockAsync(session!, request, cancellationToken));
    }

    [HttpDelete("blocks/{blockId:guid}")]
    public async Task<IActionResult> DeleteBlockAsync(Guid blockId, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken); var denied = EnsureAccess(session); if (denied is not null) return denied;
        await _appointmentService.DeleteBlockAsync(session!, blockId, cancellationToken); return NoContent();
    }

    [HttpGet("reports/summary")]
    public async Task<IActionResult> GetReportAsync([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken); var denied = EnsureAccess(session);
        return denied ?? Ok(await _appointmentService.GetReportAsync(session!, fromUtc, toUtc, cancellationToken));
    }

    [HttpGet("professionals")]
    public async Task<IActionResult> GetProfessionalsAsync(CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken); var denied = EnsureAccess(session);
        return denied ?? Ok(await _appointmentService.GetProfessionalsAsync(session!, cancellationToken));
    }

    private async Task<IActionResult> ExecuteAsync(Func<WorkspaceSessionContext, Task<AppointmentDto>> action, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        var denied = EnsureAccess(session);
        return denied ?? Ok(await action(session!));
    }

    private async Task<IActionResult> ExecuteSettingsAsync(Func<WorkspaceSessionContext, Task<AppointmentScheduleSettingsDto>> action, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken); var denied = EnsureAccess(session);
        return denied ?? Ok(await action(session!));
    }

    private Task<WorkspaceSessionContext?> GetSessionAsync(CancellationToken cancellationToken) =>
        _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);

    private ObjectResult? EnsureAccess(WorkspaceSessionContext? session)
    {
        if (session is null)
            return StatusCode(StatusCodes.Status401Unauthorized, new ProblemDetails { Title = "Sessao invalida", Status = 401 });

        return session.Capabilities.HasAppointments
            ? null
            : StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Title = "Modulo indisponivel", Detail = "Agenda nao esta disponivel para esta empresa.", Status = 403 });
    }
}
