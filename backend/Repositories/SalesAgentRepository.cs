using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Repositories.Interfaces;

namespace ZeroPaper.Repositories;

public class SalesAgentRepository : ISalesAgentRepository
{
    private readonly ZeroPaperDbContext _context;

    public SalesAgentRepository(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SalesAgent agent, CancellationToken cancellationToken = default)
        => await _context.SalesAgents.AddAsync(agent, cancellationToken);

    public Task<SalesAgent?> GetByIdAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
        => _context.SalesAgents
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);

    public Task<SalesAgent?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => _context.SalesAgents
            .FirstOrDefaultAsync(x => x.Code == code && x.IsActive, cancellationToken);

    public Task<SalesAgent?> GetByCodeWithCompanyAsync(string code, CancellationToken cancellationToken = default)
        => _context.SalesAgents
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Code == code && x.IsActive, cancellationToken);

    public async Task<IReadOnlyList<SalesAgent>> GetByCompanyAsync(Guid companyId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.SalesAgents.Where(x => x.CompanyId == companyId);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }
}
