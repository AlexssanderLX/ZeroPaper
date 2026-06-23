using ZeroPaper.Domain.Entities;

namespace ZeroPaper.Services.Interfaces;

public interface ICashOrderTableService
{
    Task<DiningTable> EnsureAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default);
    Task EnsureForActiveOwnersAsync(CancellationToken cancellationToken = default);
}
