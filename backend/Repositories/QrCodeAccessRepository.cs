using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Repositories.Interfaces;

namespace ZeroPaper.Repositories;

public class QrCodeAccessRepository : IQrCodeAccessRepository
{
    private readonly ZeroPaperDbContext _context;

    public QrCodeAccessRepository(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(QrCodeAccess qrCodeAccess, CancellationToken cancellationToken = default)
    {
        return _context.QrCodeAccesses.AddAsync(qrCodeAccess, cancellationToken).AsTask();
    }
}
