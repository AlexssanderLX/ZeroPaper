using ZeroPaper.DTOs.Workspace;

namespace ZeroPaper.Services.Interfaces;

public interface IPublicPetShopService
{
    Task<PublicPetShopDto> GetAsync(string publicCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicPetShopServiceDto>> GetServicesAsync(string publicCode, CancellationToken cancellationToken = default);
    Task<AppointmentAvailabilityDto> GetAvailabilityAsync(string publicCode, DateOnly date, Guid serviceId, CancellationToken cancellationToken = default);
    Task<PublicAppointmentCreatedDto> CreateRequestAsync(string publicCode, PublicAppointmentRequestDto request, CancellationToken cancellationToken = default);
    Task<PublicAppointmentTrackingDto> GetTrackingAsync(string accessToken, CancellationToken cancellationToken = default);
    Task<PublicAppointmentTrackingDto> CancelAsync(string accessToken, CancellationToken cancellationToken = default);
}
