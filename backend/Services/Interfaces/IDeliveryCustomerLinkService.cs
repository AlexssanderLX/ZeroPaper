using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IDeliveryCustomerLinkService
{
    string CreateToken(Guid companyId, string publicCode, string phone);
    bool TryReadToken(string? token, out DeliveryCustomerLinkPayload payload);
    Task<string?> GetOrCreateShortCodeForCustomerAsync(Guid companyId, string phone, CancellationToken cancellationToken = default);
    Task<DeliveryCustomerLinkPayload?> TryReadShortCodeAsync(string? code, CancellationToken cancellationToken = default);
}
