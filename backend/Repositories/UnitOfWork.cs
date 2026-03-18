using ZeroPaper.Data;
using ZeroPaper.Repositories.Interfaces;

namespace ZeroPaper.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ZeroPaperDbContext _context;

    public UnitOfWork(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
