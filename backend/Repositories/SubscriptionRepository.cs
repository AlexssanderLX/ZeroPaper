using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Repositories.Interfaces;

namespace ZeroPaper.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ZeroPaperDbContext _context;

    public SubscriptionRepository(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        return _context.Subscriptions.AddAsync(subscription, cancellationToken).AsTask();
    }
}
