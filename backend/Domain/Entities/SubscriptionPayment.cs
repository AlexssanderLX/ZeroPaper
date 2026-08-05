using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public sealed class SubscriptionPayment : TenantOwnedEntity
{
    private SubscriptionPayment() { }
    public SubscriptionPayment(Guid tenantId, Guid subscriptionId, string source, string externalPaymentId, decimal amount, DateTime paidAtUtc, Guid? confirmedByUserId = null) : base(tenantId)
    {
        SubscriptionId = subscriptionId;
        Source = source.Trim();
        ExternalPaymentId = externalPaymentId.Trim();
        Amount = decimal.Round(amount, 2);
        PaidAtUtc = paidAtUtc;
        ConfirmedByUserId = confirmedByUserId;
    }
    public Guid SubscriptionId { get; private set; }
    public string Source { get; private set; } = null!;
    public string ExternalPaymentId { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public DateTime PaidAtUtc { get; private set; }
    public Guid? ConfirmedByUserId { get; private set; }
}
