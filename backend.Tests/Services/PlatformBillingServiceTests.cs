using System.Net;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services;
using ZeroPaper.Services.Models;
using Xunit;

namespace ZeroPaper.Tests.Services;

public sealed class PlatformBillingServiceTests
{
    private const string AccessToken = "APP_USR-1234567890123456789012345678901234567890";

    [Fact]
    public async Task ConfigureAsync_ValidatesAccountAndNeverStoresPlainToken()
    {
        await using var context = CreateContext();
        var (root, session, hasher) = await SeedRootAsync(context);
        var handler = new StubHandler(HttpStatusCode.OK, "{\"id\":123456,\"email\":\"billing@example.com\"}");
        var service = CreateService(context, hasher, handler);

        var status = await service.ConfigureAsync(session, new ConfigureAdminPlatformBillingRequestDto
        {
            AccessToken = AccessToken,
            Password = "root-password"
        });

        var stored = await context.PlatformBillingConfigurations.SingleAsync();
        Assert.True(status.Configured);
        Assert.True(status.LiveMode);
        Assert.Equal("123456", status.AccountUserId);
        Assert.DoesNotContain(AccessToken, stored.AccessTokenCipherText, StringComparison.Ordinal);
        Assert.NotEqual(AccessToken, stored.AccessTokenCipherText);
        Assert.Equal(root.Id, stored.UpdatedByUserId);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task ConfigureAsync_WithWrongRootPassword_DoesNotCallMercadoPagoOrPersistToken()
    {
        await using var context = CreateContext();
        var (_, session, hasher) = await SeedRootAsync(context);
        var handler = new StubHandler(HttpStatusCode.OK, "{\"id\":123456}");
        var service = CreateService(context, hasher, handler);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ConfigureAsync(session, new ConfigureAdminPlatformBillingRequestDto
        {
            AccessToken = AccessToken,
            Password = "wrong-password"
        }));

        Assert.Equal(0, handler.RequestCount);
        Assert.Empty(context.PlatformBillingConfigurations);
    }

    private static ZeroPaperDbContext CreateContext() => new(new DbContextOptionsBuilder<ZeroPaperDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(AppUser Root, WorkspaceSessionContext Session, PasswordHasher Hasher)> SeedRootAsync(ZeroPaperDbContext context)
    {
        var tenantId = Guid.NewGuid();
        var company = new Company(tenantId, "ZeroPaper", "ZeroPaper", $"root-{Guid.NewGuid():N}");
        var hasher = new PasswordHasher();
        var root = new AppUser(tenantId, company.Id, "Root", "root@zeropaper.local", hasher.Hash("root-password"), UserRole.Root);
        context.Add(company);
        context.Add(root);
        await context.SaveChangesAsync();
        return (root, new WorkspaceSessionContext
        {
            TenantId = tenantId,
            CompanyId = company.Id,
            UserId = root.Id,
            Role = UserRole.Root.ToString(),
            Email = root.Email
        }, hasher);
    }

    private static PlatformBillingService CreateService(ZeroPaperDbContext context, PasswordHasher hasher, HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.mercadopago.com/") };
        return new PlatformBillingService(
            context,
            hasher,
            new EphemeralDataProtectionProvider(),
            client,
            Options.Create(new PublicAppOptions { BaseUrl = "https://zeropaperflow.com.br" }));
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal(AccessToken, request.Headers.Authorization?.Parameter);
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
