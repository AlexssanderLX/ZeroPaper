using ZeroPaper.Domain.Enums;
using ZeroPaper.Domain.Plans;
using Xunit;

namespace ZeroPaper.Tests.Services;

public sealed class SegmentCommercialPlanCatalogTests
{
    [Fact]
    public void PetShopPlan_IsResolvedBySegmentAndServerKey()
    {
        var plan = SegmentCommercialPlanCatalog.Resolve(BusinessSegment.PetShop, "operacao");

        Assert.Equal("ZeroPaper Pet Operacao", plan.Name);
        Assert.Equal(120m, plan.MonthlyPrice);
        Assert.Equal(5, plan.DefaultMaxUsers);
        Assert.False(plan.IncludesTablesModule);
        Assert.False(plan.IncludesKitchenModule);
        Assert.True(plan.IncludesMenuModule);
        Assert.True(plan.IncludesCashModule);
    }

    [Fact]
    public void PlanFromAnotherSegment_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            SegmentCommercialPlanCatalog.Resolve(BusinessSegment.PetShop, "plano-inventado"));
    }
}
