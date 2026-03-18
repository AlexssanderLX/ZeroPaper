using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Repositories.Interfaces;

namespace ZeroPaper.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly ZeroPaperDbContext _context;

    public CompanyRepository(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        return _context.Companies.AddAsync(company, cancellationToken).AsTask();
    }

    public Task<bool> AccessSlugExistsAsync(Guid tenantId, string accessSlug, CancellationToken cancellationToken = default)
    {
        return _context.Companies.AnyAsync(
            x => x.TenantId == tenantId && x.AccessSlug == accessSlug,
            cancellationToken);
    }
}
