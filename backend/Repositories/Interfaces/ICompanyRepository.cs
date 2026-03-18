using ZeroPaper.Domain.Entities;

namespace ZeroPaper.Repositories.Interfaces;

public interface ICompanyRepository
{
    Task AddAsync(Company company, CancellationToken cancellationToken = default);
    Task<bool> AccessSlugExistsAsync(Guid tenantId, string accessSlug, CancellationToken cancellationToken = default);
}
