using ZeroPaper.Domain.Entities;

namespace ZeroPaper.Services.Interfaces;

public interface IBillingNotificationService
{
    Task SendPaymentConfirmedAsync(Company company, AppUser owner, Subscription subscription, SubscriptionPayment payment, CancellationToken cancellationToken = default);
}
