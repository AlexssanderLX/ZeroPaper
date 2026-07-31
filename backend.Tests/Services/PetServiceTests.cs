using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services;
using ZeroPaper.Services.Models;
using Xunit;

namespace ZeroPaper.Tests.Services;

public sealed class PetServiceTests
{
    [Fact]
    public async Task GetById_does_not_return_pet_from_another_company()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var firstCompanyId = Guid.NewGuid();
        var secondCompanyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var pet = new Pet(tenantId, secondCompanyId, customerId, "Luna", PetSpecies.Dog, PetSize.Small);
        context.Pets.Add(pet);
        await context.SaveChangesAsync();

        var service = new PetService(context, Path.GetTempPath());
        var session = CreateSession(tenantId, firstCompanyId);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(session, pet.Id));
    }

    [Fact]
    public async Task Create_rejects_customer_from_another_company()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var firstCompanyId = Guid.NewGuid();
        var secondCompanyId = Guid.NewGuid();
        var customer = new DeliveryCustomerProfile(
            tenantId, secondCompanyId, "5511999999999", "Cliente", null, null, null, null, null, DateTime.UtcNow);
        context.DeliveryCustomerProfiles.Add(customer);
        await context.SaveChangesAsync();

        var service = new PetService(context, Path.GetTempPath());
        var request = new CreatePetRequestDto
        {
            CustomerProfileId = customer.Id,
            Name = "Luna",
            Species = PetSpecies.Dog,
            Size = PetSize.Small
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(CreateSession(tenantId, firstCompanyId), request));
    }

    [Fact]
    public async Task Restaurant_session_cannot_use_pet_service()
    {
        await using var context = CreateContext();
        var service = new PetService(context, Path.GetTempPath());
        var session = CreateSession(Guid.NewGuid(), Guid.NewGuid(), BusinessSegment.Restaurant);

        await Assert.ThrowsAsync<CapabilityUnavailableException>(() => service.GetAsync(session, null, null, null, 1, 25));
    }

    private static WorkspaceSessionContext CreateSession(
        Guid tenantId,
        Guid companyId,
        BusinessSegment businessSegment = BusinessSegment.PetShop) => new()
    {
        TenantId = tenantId,
        CompanyId = companyId,
        UserId = Guid.NewGuid(),
        BusinessSegment = businessSegment,
        Capabilities = BusinessCapabilities.Resolve(businessSegment, true, true, true, true, true, true, true, true, true, true)
    };

    private static ZeroPaperDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ZeroPaperDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ZeroPaperDbContext(options);
    }
}
