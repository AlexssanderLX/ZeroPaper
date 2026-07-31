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

public sealed class PetListDto
{
    public List<PetDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
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

public sealed class PetPhotoDto
{
    public Guid PetId { get; set; }
    public string? PhotoUrl { get; set; }
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

public sealed class CreateAppointmentOrderRequestDto
{
    public string? PaymentMethod { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Notes { get; set; }
}

public sealed class AppointmentOrderResultDto
{
    public AppointmentDto Appointment { get; set; } = new();
    public CustomerOrderDto Order { get; set; } = new();
}

public sealed class ChangeAppointmentStatusRequestDto
{
    public AppointmentStatus Status { get; set; }
    public string? CancellationReason { get; set; }
}

public sealed class AppointmentHistoryDto
{
    public Guid Id { get; set; }
    public AppointmentStatus PreviousStatus { get; set; }
    public AppointmentStatus NewStatus { get; set; }
    public Guid ChangedByUserId { get; set; }
    public string ChangedByUserName { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; }
    public string? Reason { get; set; }
}

public sealed class AppointmentScheduleSettingsDto
{
    public string ServiceDays { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int SlotIntervalMinutes { get; set; }
    public string TimeZone { get; set; } = string.Empty;
}

public sealed class UpdateAppointmentScheduleSettingsRequestDto
{
    public string ServiceDays { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int SlotIntervalMinutes { get; set; }
}

public sealed class AppointmentAvailabilityDto
{
    public DateOnly Date { get; set; }
    public Guid ServiceId { get; set; }
    public int DurationMinutes { get; set; }
    public string TimeZone { get; set; } = string.Empty;
    public List<AppointmentAvailabilitySlotDto> Slots { get; set; } = [];
}

public sealed class AppointmentAvailabilitySlotDto
{
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
}

public sealed class AppointmentBlockDto
{
    public Guid Id { get; set; }
    public Guid? AssignedUserId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public string? Reason { get; set; }
}

public sealed class CreateAppointmentBlockRequestDto
{
    public Guid? AssignedUserId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public string? Reason { get; set; }
}

public sealed class AppointmentReportDto
{
    public int Total { get; set; }
    public int Requested { get; set; }
    public int Confirmed { get; set; }
    public int InProgress { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int NoShow { get; set; }
    public decimal LinkedRevenue { get; set; }
}

public sealed class AppointmentProfessionalDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class PublicPetShopDto
{
    public string BusinessName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string TimeZone { get; set; } = string.Empty;
}

public sealed class PublicPetShopServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public string? ImageUrl { get; set; }
}

public sealed class PublicAppointmentRequestDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PetName { get; set; } = string.Empty;
    public PetSpecies PetSpecies { get; set; }
    public PetSize PetSize { get; set; }
    public string? PetBreed { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public string? Notes { get; set; }
}

public sealed class PublicAppointmentCreatedDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessExpiresAtUtc { get; set; }
    public AppointmentStatus Status { get; set; }
    public DateTime StartsAtUtc { get; set; }
}

public sealed class PublicAppointmentTrackingDto
{
    public string PetName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public AppointmentStatus Status { get; set; }
    public bool CanCancel { get; set; }
}
