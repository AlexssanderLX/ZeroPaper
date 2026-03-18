using ZeroPaper.Domain.Entities;

namespace ZeroPaper.Repositories.Interfaces;

public interface IQrCodeAccessRepository
{
    Task AddAsync(QrCodeAccess qrCodeAccess, CancellationToken cancellationToken = default);
}
