using ZeroPaper.Domain.Entities;
using ZeroPaper.DTOs.Workspace;

namespace ZeroPaper.Services.Interfaces;

public interface IDeliveryFreightService
{
    DeliveryFreightSettingsDto BuildSettings(Company company);
    Task<DeliveryFreightQuoteDto> QuoteAsync(
        Company company,
        string? destinationPostalCode,
        decimal subtotal,
        CancellationToken cancellationToken = default);
}
