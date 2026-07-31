using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IAppointmentService
{
    Task<IReadOnlyList<AppointmentDto>> GetAsync(WorkspaceSessionContext session, DateTime fromUtc, DateTime toUtc, AppointmentStatus? status, Guid? petId, Guid? customerId, Guid? assignedUserId, CancellationToken cancellationToken = default);
    Task<AppointmentDto> GetByIdAsync(WorkspaceSessionContext session, Guid appointmentId, CancellationToken cancellationToken = default);
    Task<AppointmentDto> CreateAsync(WorkspaceSessionContext session, CreateAppointmentRequestDto request, CancellationToken cancellationToken = default);
    Task<AppointmentDto> RescheduleAsync(WorkspaceSessionContext session, Guid appointmentId, RescheduleAppointmentRequestDto request, CancellationToken cancellationToken = default);
    Task<AppointmentDto> UpdateNotesAsync(WorkspaceSessionContext session, Guid appointmentId, UpdateAppointmentNotesRequestDto request, CancellationToken cancellationToken = default);
    Task<AppointmentDto> AssignAsync(WorkspaceSessionContext session, Guid appointmentId, Guid? assignedUserId, CancellationToken cancellationToken = default);
    Task<AppointmentDto> LinkOrderAsync(WorkspaceSessionContext session, Guid appointmentId, Guid customerOrderId, CancellationToken cancellationToken = default);
    Task<AppointmentOrderResultDto> CreateOrderAsync(WorkspaceSessionContext session, Guid appointmentId, CreateAppointmentOrderRequestDto request, CancellationToken cancellationToken = default);
    Task<AppointmentDto> ChangeStatusAsync(WorkspaceSessionContext session, Guid appointmentId, ChangeAppointmentStatusRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentHistoryDto>> GetHistoryAsync(WorkspaceSessionContext session, Guid appointmentId, CancellationToken cancellationToken = default);
    Task<AppointmentScheduleSettingsDto> GetSettingsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<AppointmentScheduleSettingsDto> UpdateSettingsAsync(WorkspaceSessionContext session, UpdateAppointmentScheduleSettingsRequestDto request, CancellationToken cancellationToken = default);
    Task<AppointmentAvailabilityDto> GetAvailabilityAsync(WorkspaceSessionContext session, DateOnly date, Guid serviceId, Guid? assignedUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentBlockDto>> GetBlocksAsync(WorkspaceSessionContext session, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<AppointmentBlockDto> CreateBlockAsync(WorkspaceSessionContext session, CreateAppointmentBlockRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteBlockAsync(WorkspaceSessionContext session, Guid blockId, CancellationToken cancellationToken = default);
    Task<AppointmentReportDto> GetReportAsync(WorkspaceSessionContext session, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentProfessionalDto>> GetProfessionalsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
}
