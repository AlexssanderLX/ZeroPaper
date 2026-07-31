using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Services;
using Xunit;

namespace ZeroPaper.Tests.Services;

public sealed class PublicPetShopServiceTests
{
    [Fact]
    public async Task Tracking_token_returns_only_public_contract()
    {
        await using var context = Context(); const string token = "safe-public-token";
        var tenant = Guid.NewGuid(); var company = Guid.NewGuid();
        var pet = new Pet(tenant, company, Guid.NewGuid(), "Luna", PetSpecies.Dog, PetSize.Small);
        var appointment = new Appointment(tenant, company, pet.Id, Guid.NewGuid(), DateTime.UtcNow.AddDays(1), 60, "Banho", 80m);
        appointment.SetPublicAccess(Hash(token), DateTime.UtcNow.AddDays(1));
        context.Pets.Add(pet); context.Appointments.Add(appointment); await context.SaveChangesAsync();
        var service = new PublicPetShopService(context, new AppointmentService(context));
        var result = await service.GetTrackingAsync(token);
        Assert.Equal("Luna", result.PetName); Assert.Equal("Banho", result.ServiceName); Assert.True(result.CanCancel);
    }

    [Fact]
    public async Task Invalid_tracking_token_is_not_found()
    {
        await using var context = Context(); var service = new PublicPetShopService(context, new AppointmentService(context));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetTrackingAsync("invalid"));
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static ZeroPaperDbContext Context() => new(new DbContextOptionsBuilder<ZeroPaperDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
