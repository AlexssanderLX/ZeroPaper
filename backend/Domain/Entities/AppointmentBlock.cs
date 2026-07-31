using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public sealed class AppointmentBlock : TenantOwnedEntity
{
    private AppointmentBlock() { }

    public AppointmentBlock(Guid tenantId, Guid companyId, DateTime startsAtUtc, DateTime endsAtUtc, Guid? assignedUserId, string? reason) : base(tenantId)
    {
        if (startsAtUtc.Kind != DateTimeKind.Utc || endsAtUtc.Kind != DateTimeKind.Utc || endsAtUtc <= startsAtUtc)
            throw new ArgumentException("Informe um intervalo UTC valido.");
        CompanyId = companyId;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        AssignedUserId = assignedUserId;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim().Length <= 300 ? reason.Trim() : throw new ArgumentException("O motivo deve ter no maximo 300 caracteres.", nameof(reason));
    }

    public Guid CompanyId { get; private set; }
    public Guid? AssignedUserId { get; private set; }
    public DateTime StartsAtUtc { get; private set; }
    public DateTime EndsAtUtc { get; private set; }
    public string? Reason { get; private set; }
    public Company Company { get; private set; } = null!;
    public AppUser? AssignedUser { get; private set; }
}
