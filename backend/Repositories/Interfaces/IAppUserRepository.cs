using ZeroPaper.Domain.Entities;

namespace ZeroPaper.Repositories.Interfaces;

public interface IAppUserRepository
{
    Task AddAsync(AppUser user, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(Guid tenantId, string email, CancellationToken cancellationToken = default);
}
