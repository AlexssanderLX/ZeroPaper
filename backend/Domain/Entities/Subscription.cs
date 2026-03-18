using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public class Subscription : TenantOwnedEntity
{
    private Subscription()
    {
    }

    public Subscription(
        Guid tenantId,
        string planName,
        decimal monthlyPrice,
        int maxUsers,
        DateTime startsAtUtc,
        SubscriptionStatus status = SubscriptionStatus.Trial) : base(tenantId)
    {
        ChangePlan(planName, monthlyPrice, maxUsers);
        StartsAtUtc = startsAtUtc;
        Status = status;
    }

    public string PlanName { get; private set; } = null!;
    public decimal MonthlyPrice { get; private set; }
    public int MaxUsers { get; private set; }
    public DateTime StartsAtUtc { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }
    public SubscriptionStatus Status { get; private set; }

    public Tenant Tenant { get; private set; } = null!;

    public void ChangePlan(string planName, decimal monthlyPrice, int maxUsers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(planName);

        if (monthlyPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyPrice));
        }

        if (maxUsers <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxUsers));
        }

        PlanName = planName.Trim();
        MonthlyPrice = decimal.Round(monthlyPrice, 2);
        MaxUsers = maxUsers;
        Touch();
    }

    public void Reactivate()
    {
        Status = SubscriptionStatus.Active;
        EndsAtUtc = null;
        Touch();
    }

    public void Suspend()
    {
        Status = SubscriptionStatus.Suspended;
        Touch();
    }

    public void Cancel(DateTime endsAtUtc)
    {
        if (endsAtUtc < StartsAtUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(endsAtUtc));
        }

        Status = SubscriptionStatus.Cancelled;
        EndsAtUtc = endsAtUtc;
        Touch();
    }
}
