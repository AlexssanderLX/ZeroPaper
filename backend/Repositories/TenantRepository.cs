using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Repositories.Interfaces;

namespace ZeroPaper.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly ZeroPaperDbContext _context;

    public TenantRepository(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        return _context.Tenants.AddAsync(tenant, cancellationToken).AsTask();
    }

    public Task<bool> IdentifierExistsAsync(string identifier, CancellationToken cancellationToken = default)
    {
        return _context.Tenants.AnyAsync(x => x.Identifier == identifier, cancellationToken);
    }
}
