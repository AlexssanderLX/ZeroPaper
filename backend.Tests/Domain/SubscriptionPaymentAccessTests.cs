using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using Xunit;

namespace ZeroPaper.Tests.Domain;

public sealed class SubscriptionPaymentAccessTests
{
    [Fact]
    public void FirstPayment_GrantsExactlyOneMonthFromPaymentDate()
    {
        var subscription = CreateSubscription();
        var paidAt = new DateTime(2026, 8, 5, 14, 30, 0, DateTimeKind.Utc);
        Assert.Equal(paidAt.AddMonths(1), subscription.RegisterPaidMonth(paidAt));
        Assert.True(subscription.HasPaidAccess(paidAt.AddDays(20)));
        Assert.False(subscription.HasPaidAccess(paidAt.AddMonths(1)));
    }

    [Fact]
    public void EarlyRenewal_ExtendsFromCurrentExpirationWithoutLosingDays()
    {
        var subscription = CreateSubscription();
        var firstPayment = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc);
        subscription.RegisterPaidMonth(firstPayment);
        Assert.Equal(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc), subscription.RegisterPaidMonth(firstPayment.AddDays(20)));
    }

    [Fact]
    public void LateRenewal_StartsNewMonthFromNewPaymentDate()
    {
        var subscription = CreateSubscription();
        var firstPayment = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc);
        subscription.RegisterPaidMonth(firstPayment);
        var latePayment = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc), subscription.RegisterPaidMonth(latePayment));
    }

    private static Subscription CreateSubscription() => new(Guid.NewGuid(), "ZeroPaper Operacao", 120m, 5, DateTime.UtcNow, SubscriptionStatus.Active);
}
