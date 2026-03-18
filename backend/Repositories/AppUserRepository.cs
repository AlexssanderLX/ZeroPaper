using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Repositories.Interfaces;

namespace ZeroPaper.Repositories;

public class AppUserRepository : IAppUserRepository
{
    private readonly ZeroPaperDbContext _context;

    public AppUserRepository(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        return _context.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public Task<bool> EmailExistsAsync(Guid tenantId, string email, CancellationToken cancellationToken = default)
    {
        return _context.Users.AnyAsync(
            x => x.TenantId == tenantId && x.Email == email,
            cancellationToken);
    }
}
