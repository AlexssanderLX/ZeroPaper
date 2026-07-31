using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public sealed class AppointmentStatusHistory : TenantOwnedEntity
{
    private AppointmentStatusHistory() { }

    public AppointmentStatusHistory(Guid tenantId, Guid companyId, Guid appointmentId, Guid changedByUserId,
        AppointmentStatus previousStatus, AppointmentStatus newStatus, DateTime changedAtUtc, string? reason = null) : base(tenantId)
    {
        CompanyId = companyId;
        AppointmentId = appointmentId;
        ChangedByUserId = changedByUserId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedAtUtc = changedAtUtc.Kind == DateTimeKind.Utc ? changedAtUtc : throw new ArgumentException("A data deve estar em UTC.", nameof(changedAtUtc));
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim().Length <= 500 ? reason.Trim() : throw new ArgumentException("O motivo deve ter no maximo 500 caracteres.", nameof(reason));
    }

    public Guid CompanyId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public AppointmentStatus PreviousStatus { get; private set; }
    public AppointmentStatus NewStatus { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
    public string? Reason { get; private set; }
    public Appointment Appointment { get; private set; } = null!;
    public AppUser ChangedByUser { get; private set; } = null!;
}
