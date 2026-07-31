using ZeroPaper.Domain.Enums;

namespace ZeroPaper.DTOs.Workspace;

public sealed class PetDto
{
    public Guid Id { get; set; }
    public Guid CustomerProfileId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PetSpecies Species { get; set; }
    public PetSize Size { get; set; }
    public string? Breed { get; set; }
    public decimal? WeightKg { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? BehaviorNotes { get; set; }
    public string? AllergyNotes { get; set; }
    public string? Restrictions { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreatePetRequestDto
{
    public Guid CustomerProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PetSpecies Species { get; set; }
    public PetSize Size { get; set; }
    public string? Breed { get; set; }
    public decimal? WeightKg { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? BehaviorNotes { get; set; }
    public string? AllergyNotes { get; set; }
    public string? Restrictions { get; set; }
}

public sealed class UpdatePetRequestDto
{
    public string Name { get; set; } = string.Empty;
    public PetSpecies Species { get; set; }
    public PetSize Size { get; set; }
    public string? Breed { get; set; }
    public decimal? WeightKg { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? BehaviorNotes { get; set; }
    public string? AllergyNotes { get; set; }
    public string? Restrictions { get; set; }
}

public sealed class UpdatePetStatusRequestDto
{
    public bool IsActive { get; set; }
}

public sealed class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid PetId { get; set; }
    public string PetName { get; set; } = string.Empty;
    public Guid MenuItemId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public Guid? CustomerOrderId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public AppointmentStatus Status { get; set; }
    public decimal UnitPrice { get; set; }
    public string? CustomerNotes { get; set; }
    public string? InternalNotes { get; set; }
    public string? CancellationReason { get; set; }
}

public sealed class CreateAppointmentRequestDto
{
    public Guid PetId { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public int? DurationMinutes { get; set; }
    public string? CustomerNotes { get; set; }
}

public sealed class RescheduleAppointmentRequestDto
{
    public DateTime StartsAtUtc { get; set; }
    public int DurationMinutes { get; set; }
}

public sealed class UpdateAppointmentNotesRequestDto
{
    public string? CustomerNotes { get; set; }
    public string? InternalNotes { get; set; }
}

public sealed class AssignAppointmentRequestDto
{
    public Guid? AssignedUserId { get; set; }
}

public sealed class LinkAppointmentOrderRequestDto
{
    public Guid CustomerOrderId { get; set; }
}

public sealed class ChangeAppointmentStatusRequestDto
{
    public AppointmentStatus Status { get; set; }
    public string? CancellationReason { get; set; }
}
