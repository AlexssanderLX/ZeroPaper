using ZeroPaper.Domain.Enums;
using ZeroPaper.Services.Models;
using Xunit;

namespace ZeroPaper.Tests.Services;

public sealed class BusinessCapabilitiesTests
{
    [Fact]
    public void Restaurant_keeps_restaurant_capabilities_without_pet_modules()
    {
        var result = BusinessCapabilities.Resolve(BusinessSegment.Restaurant, true, true, true, true, true, true, true, true, true, true);
        Assert.True(result.HasTables); Assert.True(result.HasKitchen); Assert.True(result.HasDelivery);
        Assert.False(result.HasPets); Assert.False(result.HasAppointments);
    }

    [Fact]
    public void PetShop_gets_pet_modules_without_restaurant_operation_modules()
    {
        var result = BusinessCapabilities.Resolve(BusinessSegment.PetShop, true, true, true, true, true, true, true, true, true, true);
        Assert.True(result.HasPets); Assert.True(result.HasAppointments); Assert.True(result.HasCatalog);
        Assert.False(result.HasTables); Assert.False(result.HasKitchen); Assert.False(result.HasDelivery);
    }
}
