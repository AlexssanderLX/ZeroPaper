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
        Assert.Equal(ProductReleaseStage.Beta, product.ReleaseStage);
        Assert.False(product.RequiresPayment);
        Assert.Equal(5, product.DefaultMaxUsers);
    }

    [Fact]
    public void PlanFromAnotherSegment_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            SubscriptionProductCatalog.ResolvePet("produto-inventado"));
    }

    [Fact]
    public void HostingProduct_IsResolvedWithoutCommercialPrice()
    {
        var product = SubscriptionProductCatalog.Resolve(SubscriptionProductType.PetHosting);
        Assert.Equal("ZeroPaper Hospedagem", product.Name);
        Assert.Equal(ProductReleaseStage.Beta, product.ReleaseStage);
        Assert.False(product.RequiresPayment);
        Assert.Equal(5, product.DefaultMaxUsers);
    }
}
