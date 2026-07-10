using ZeroPaper.Domain.Entities;

namespace ZeroPaper.Repositories.Interfaces;

public interface ISalesAgentRepository
{
    Task AddAsync(SalesAgent agent, CancellationToken cancellationToken = default);
    Task<SalesAgent?> GetByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<SalesAgent?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<SalesAgent?> GetByCodeWithCompanyAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesAgent>> GetByCompanyAsync(Guid companyId, bool includeInactive = false, CancellationToken cancellationToken = default);
}
