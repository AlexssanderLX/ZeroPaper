using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public sealed class Appointment : TenantOwnedEntity
{
    private const int MaxServiceNameLength = 160;
    private const int MaxNotesLength = 1000;
    private const int MaxCancellationReasonLength = 500;

    private Appointment()
    {
        // Necessário para o EF Core.
    }

    public Appointment(
        Guid tenantId,
        Guid companyId,
        Guid petId,
        Guid menuItemId,
        DateTime startsAtUtc,
        int durationMinutes,
        string serviceNameSnapshot,
        decimal unitPriceSnapshot,
        string? customerNotes = null)
        : base(tenantId)
    {
        ValidateRequiredId(companyId, nameof(companyId));
        ValidateRequiredId(petId, nameof(petId));
        ValidateRequiredId(menuItemId, nameof(menuItemId));

        if (startsAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "O horário do agendamento deve estar em UTC.",
                nameof(startsAtUtc));
        }

        if (durationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationMinutes),
                "A duração deve ser maior que zero.");
        }

        if (unitPriceSnapshot < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPriceSnapshot),
                "O preço não pode ser negativo.");
        }

        CompanyId = companyId;
        PetId = petId;
        MenuItemId = menuItemId;
        StartsAtUtc = startsAtUtc;
        DurationMinutes = durationMinutes;

        ServiceNameSnapshot = NormalizeRequiredText(
            serviceNameSnapshot,
            MaxServiceNameLength,
            nameof(serviceNameSnapshot));

        UnitPriceSnapshot = unitPriceSnapshot;

        CustomerNotes = NormalizeOptionalText(
            customerNotes,
            MaxNotesLength,
            nameof(customerNotes));

        Status = AppointmentStatus.Requested;
    }

    public Guid CompanyId { get; private set; }

    public Guid PetId { get; private set; }

    public Guid MenuItemId { get; private set; }

    public Guid? CustomerOrderId { get; private set; }

    public Guid? AssignedUserId { get; private set; }

    public DateTime StartsAtUtc { get; private set; }

    public int DurationMinutes { get; private set; }

    public DateTime EndsAtUtc => StartsAtUtc.AddMinutes(DurationMinutes);

    public AppointmentStatus Status { get; private set; }

    public string ServiceNameSnapshot { get; private set; } = string.Empty;

    public decimal UnitPriceSnapshot { get; private set; }

    public string? CustomerNotes { get; private set; }

    public string? InternalNotes { get; private set; }

    public string? CancellationReason { get; private set; }

    public DateTime? ConfirmedAtUtc { get; private set; }

    public DateTime? StartedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public DateTime? NoShowAtUtc { get; private set; }

    public void Reschedule(DateTime startsAtUtc, int durationMinutes)
    {
        EnsureNotTerminal();

        if (Status == AppointmentStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Um atendimento em andamento não pode ser reagendado.");
        }

        if (startsAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "O horário do agendamento deve estar em UTC.",
                nameof(startsAtUtc));
        }

        if (durationMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationMinutes),
                "A duração deve ser maior que zero.");
        }

        StartsAtUtc = startsAtUtc;
        DurationMinutes = durationMinutes;

        Touch();
    }

    public void Confirm(DateTime confirmedAtUtc)
    {
        EnsureStatus(AppointmentStatus.Requested);

        ConfirmedAtUtc = EnsureUtc(confirmedAtUtc, nameof(confirmedAtUtc));
        Status = AppointmentStatus.Confirmed;

        Touch();
    }

    public void Start(DateTime startedAtUtc)
    {
        EnsureStatus(AppointmentStatus.Confirmed);

        var normalizedStartedAtUtc = EnsureUtc(
            startedAtUtc,
            nameof(startedAtUtc));

        if (ConfirmedAtUtc.HasValue &&
            normalizedStartedAtUtc < ConfirmedAtUtc.Value)
        {
            throw new InvalidOperationException(
                "O atendimento não pode começar antes da confirmação.");
        }

        StartedAtUtc = normalizedStartedAtUtc;
        Status = AppointmentStatus.InProgress;

        Touch();
    }

    public void Complete(DateTime completedAtUtc)
    {
        EnsureStatus(AppointmentStatus.InProgress);

        var normalizedCompletedAtUtc = EnsureUtc(
            completedAtUtc,
            nameof(completedAtUtc));

        if (StartedAtUtc.HasValue &&
            normalizedCompletedAtUtc < StartedAtUtc.Value)
        {
            throw new InvalidOperationException(
                "O atendimento não pode terminar antes de começar.");
        }

        CompletedAtUtc = normalizedCompletedAtUtc;
        Status = AppointmentStatus.Completed;

        Touch();
    }

    public void Cancel(string reason, DateTime cancelledAtUtc)
    {
        EnsureNotTerminal();

        if (Status == AppointmentStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Um atendimento em andamento não pode ser cancelado.");
        }

        CancellationReason = NormalizeRequiredText(
            reason,
            MaxCancellationReasonLength,
            nameof(reason));

        CancelledAtUtc = EnsureUtc(
            cancelledAtUtc,
            nameof(cancelledAtUtc));

        Status = AppointmentStatus.Cancelled;

        Touch();
    }

    public void MarkAsNoShow(DateTime noShowAtUtc)
    {
        EnsureStatus(AppointmentStatus.Confirmed);

        NoShowAtUtc = EnsureUtc(noShowAtUtc, nameof(noShowAtUtc));
        Status = AppointmentStatus.NoShow;

        Touch();
    }

    public void AssignUser(Guid? assignedUserId)
    {
        EnsureNotTerminal();

        if (assignedUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador do profissional é inválido.",
                nameof(assignedUserId));
        }

        AssignedUserId = assignedUserId;

        Touch();
    }

    public void LinkCustomerOrder(Guid customerOrderId)
    {
        EnsureNotTerminal();
        ValidateRequiredId(customerOrderId, nameof(customerOrderId));

        CustomerOrderId = customerOrderId;

        Touch();
    }

    public void UpdateNotes(
        string? customerNotes,
        string? internalNotes)
    {
        EnsureNotTerminal();

        CustomerNotes = NormalizeOptionalText(
            customerNotes,
            MaxNotesLength,
            nameof(customerNotes));

        InternalNotes = NormalizeOptionalText(
            internalNotes,
            MaxNotesLength,
            nameof(internalNotes));

        Touch();
    }

    private void EnsureStatus(AppointmentStatus expectedStatus)
    {
        if (Status != expectedStatus)
        {
            throw new InvalidOperationException(
                $"A operação exige o status {expectedStatus}, mas o agendamento está em {Status}.");
        }
    }

    private void EnsureNotTerminal()
    {
        if (Status is AppointmentStatus.Completed
            or AppointmentStatus.Cancelled
            or AppointmentStatus.NoShow)
        {
            throw new InvalidOperationException(
                "O agendamento está em um estado final e não pode mais ser alterado.");
        }
    }

    private static DateTime EnsureUtc(
        DateTime value,
        string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "A data deve estar em UTC.",
                parameterName);
        }

        return value;
    }

    private static void ValidateRequiredId(
        Guid value,
        string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador é obrigatório.",
                parameterName);
        }
    }

    private static string NormalizeRequiredText(
        string value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "O valor é obrigatório.",
                parameterName);
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"O valor deve possuir no máximo {maxLength} caracteres.",
                parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(
        string? value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"O valor deve possuir no máximo {maxLength} caracteres.",
                parameterName);
        }

        return normalized;
    }
}