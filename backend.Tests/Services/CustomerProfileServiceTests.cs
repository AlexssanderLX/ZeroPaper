using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services;
using ZeroPaper.Services.Models;
using Xunit;

namespace ZeroPaper.Tests.Services;

public sealed class CustomerProfileServiceTests
{
    [Fact]
    public async Task Customer_from_another_company_is_not_visible()
    {
        await using var context = Context(); var tenant = Guid.NewGuid();
        var profile = new DeliveryCustomerProfile(tenant, Guid.NewGuid(), "5511999999999", "Ana", null, null, null, null, null, DateTime.UtcNow);
        context.DeliveryCustomerProfiles.Add(profile); await context.SaveChangesAsync();
        var service = new CustomerProfileService(context);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(Session(tenant, Guid.NewGuid()), profile.Id));
    }

    [Fact]
    public async Task Duplicate_phone_in_same_company_is_rejected()
    {
        await using var context = Context(); var tenant = Guid.NewGuid(); var company = Guid.NewGuid();
        context.DeliveryCustomerProfiles.Add(new DeliveryCustomerProfile(tenant, company, "5511999999999", "Ana", null, null, null, null, null, DateTime.UtcNow));
        await context.SaveChangesAsync(); var service = new CustomerProfileService(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(Session(tenant, company), new CreateCustomerProfileRequestDto { PhoneNumber = "11999999999", Name = "Outra" }));
    }

    private static WorkspaceSessionContext Session(Guid tenant, Guid company) => new()
    { TenantId = tenant, CompanyId = company, BusinessSegment = BusinessSegment.PetShop, Capabilities = BusinessCapabilities.Resolve(BusinessSegment.PetShop, true, true, true, true, true, true, true, true, true, true) };
    private static ZeroPaperDbContext Context() => new(new DbContextOptionsBuilder<ZeroPaperDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
