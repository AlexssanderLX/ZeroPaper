using ZeroPaper.Domain.Enums;
using ZeroPaper.Domain.Plans;
using Xunit;

namespace ZeroPaper.Tests.Services;

public sealed class SegmentCommercialPlanCatalogTests
{
    [Fact]
    public void PetShopProduct_IsResolvedByServerKey()
    {
        var product = SubscriptionProductCatalog.ResolvePet("pet-shop");

        Assert.Equal(SubscriptionProductType.PetShop, product.Type);
        Assert.Equal("ZeroPaper Pet Shop", product.Name);
        Assert.Equal(150m, product.MonthlyPrice);
        Assert.Equal(5, product.DefaultMaxUsers);
    }

    [Fact]
    public void PlanFromAnotherSegment_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            SubscriptionProductCatalog.ResolvePet("produto-inventado"));
    }

    [Fact]
    public void HostingProduct_HasSingleCanonicalPrice()
    {
        var product = SubscriptionProductCatalog.Resolve(SubscriptionProductType.PetHosting);
        Assert.Equal("ZeroPaper Hospedagem", product.Name);
        Assert.Equal(100m, product.MonthlyPrice);
    }
}
