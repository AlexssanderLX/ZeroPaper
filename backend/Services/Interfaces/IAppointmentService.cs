using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IAppointmentService
{
    Task<IReadOnlyList<AppointmentDto>> GetAsync(WorkspaceSessionContext session, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<AppointmentDto> GetByIdAsync(WorkspaceSessionContext session, Guid appointmentId, CancellationToken cancellationToken = default);
    Task<AppointmentDto> CreateAsync(WorkspaceSessionContext session, CreateAppointmentRequestDto request, CancellationToken cancellationToken = default);
    Task<AppointmentDto> RescheduleAsync(WorkspaceSessionContext session, Guid appointmentId, RescheduleAppointmentRequestDto request, CancellationToken cancellationToken = default);
    Task<AppointmentDto> UpdateNotesAsync(WorkspaceSessionContext session, Guid appointmentId, UpdateAppointmentNotesRequestDto request, CancellationToken cancellationToken = default);
    Task<AppointmentDto> AssignAsync(WorkspaceSessionContext session, Guid appointmentId, Guid? assignedUserId, CancellationToken cancellationToken = default);
    Task<AppointmentDto> LinkOrderAsync(WorkspaceSessionContext session, Guid appointmentId, Guid customerOrderId, CancellationToken cancellationToken = default);
    Task<AppointmentDto> ChangeStatusAsync(WorkspaceSessionContext session, Guid appointmentId, ChangeAppointmentStatusRequestDto request, CancellationToken cancellationToken = default);
}
