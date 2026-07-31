using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services;
using ZeroPaper.Services.Models;
using Xunit;

namespace ZeroPaper.Tests.Services;

public sealed class AppointmentServiceTests
{
    [Fact]
    public async Task GetById_does_not_return_appointment_from_another_company()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var ownerCompanyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var pet = new Pet(tenantId, otherCompanyId, Guid.NewGuid(), "Luna", PetSpecies.Dog, PetSize.Small);
        var appointment = new Appointment(
            tenantId, otherCompanyId, pet.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(1), 60, "Banho", 80m);
        context.Pets.Add(pet);
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var service = new AppointmentService(context);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetByIdAsync(CreateSession(tenantId, ownerCompanyId), appointment.Id));
    }

    [Fact]
    public async Task Create_rejects_service_from_another_company()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var pet = new Pet(tenantId, companyId, Guid.NewGuid(), "Luna", PetSpecies.Dog, PetSize.Small);
        var category = new MenuCategory(tenantId, Guid.NewGuid(), "Servicos");
        var foreignService = new MenuItem(
            tenantId, category.CompanyId, category.Id, "Banho", 80m, kind: CatalogItemKind.Service, estimatedDurationMinutes: 60);
        context.Pets.Add(pet);
        context.MenuCategories.Add(category);
        context.MenuItems.Add(foreignService);
        await context.SaveChangesAsync();

        var service = new AppointmentService(context);
        var request = new CreateAppointmentRequestDto
        {
            PetId = pet.Id,
            MenuItemId = foreignService.Id,
            StartsAtUtc = DateTime.UtcNow.AddDays(1)
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(CreateSession(tenantId, companyId), request));
    }

    private static WorkspaceSessionContext CreateSession(Guid tenantId, Guid companyId) => new()
    {
        TenantId = tenantId,
        CompanyId = companyId,
        UserId = Guid.NewGuid(),
        BusinessSegment = BusinessSegment.PetShop,
        Capabilities = BusinessCapabilities.Resolve(BusinessSegment.PetShop, true, true, true, true, true, true, true, true, true, true)
    };

    private static ZeroPaperDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ZeroPaperDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ZeroPaperDbContext(options);
    }
}
