using ZeroPaper.Domain.Entities;

namespace ZeroPaper.Repositories.Interfaces;

public interface ITenantRepository
{
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task<bool> IdentifierExistsAsync(string identifier, CancellationToken cancellationToken = default);
}
