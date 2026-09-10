using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Auth;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;
using Xunit;

namespace ZeroPaper.Tests.Services;

public sealed class AuthSessionServiceTests
{
    [Fact]
    public async Task LoginAsync_WithCorrectPasswordForPendingAccount_ReturnsPendingState()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher();
        var user = CreateOwner(hasher, isActive: false);
        context.Add(user.Company);
        context.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context, hasher);

        var exception = await Assert.ThrowsAsync<AccountPendingApprovalException>(() => service.LoginAsync(new LoginRequestDto
        {
            Email = user.Email,
            Password = "correct-password"
        }));

        Assert.Equal(AccountPendingApprovalException.ErrorCode, "ACCOUNT_PENDING_APPROVAL");
        Assert.Contains("análise", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPasswordForPendingAccount_DoesNotRevealPendingState()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher();
        var user = CreateOwner(hasher, isActive: false);
        context.Add(user.Company);
        context.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context, hasher);

        var result = await service.LoginAsync(new LoginRequestDto
        {
            Email = user.Email,
            Password = "wrong-password"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_DoesNotRevealPendingState()
    {
        await using var context = CreateContext();
        var service = CreateService(context, new PasswordHasher());

        var result = await service.LoginAsync(new LoginRequestDto
        {
            Email = "unknown@example.com",
            Password = "any-password"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_WithDisabledExistingAccount_DoesNotClaimApprovalIsPending()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher();
        var user = CreateOwner(hasher, isActive: true);
        user.RegisterLogin();
        user.Deactivate();
        context.Add(user.Company);
        context.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context, hasher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync(new LoginRequestDto
        {
            Email = user.Email,
            Password = "correct-password"
        }));

        Assert.IsNotType<AccountPendingApprovalException>(exception);
        Assert.Contains("indisponivel", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DemoAccount_RejectsPasswordLogin_AndRequiresActiveDemoLink()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher();
        var tenantId = Guid.NewGuid();
        var company = new Company(tenantId, "Demo", "Restaurante Demo", $"demo-{Guid.NewGuid():N}");
        company.MarkAsDemoAccount();
        var owner = new AppUser(tenantId, company.Id, "Visitante", "demo@example.invalid", hasher.Hash("known-password"), UserRole.Owner);
        var rawLinkToken = new string('a', 64);
        var link = new DemoAccessLink(tenantId, company.Id, owner.Id, ComputeTokenHash(rawLinkToken), Guid.NewGuid());
        context.AddRange(company, owner, link);
        await context.SaveChangesAsync();

        var service = CreateService(context, hasher, new AcceptingCashOrderTableService());
        var passwordLogin = await service.LoginAsync(new LoginRequestDto
        {
            Email = owner.Email,
            Password = "known-password"
        });
        Assert.Null(passwordLogin);

        var demoLogin = await service.LoginWithDemoAsync(new DemoLoginRequestDto { Token = rawLinkToken });
        Assert.NotNull(demoLogin);
        Assert.True(demoLogin.IsDemoAccount);

        var activeSession = await service.GetSessionAsync($"Bearer {demoLogin.Token}");
        Assert.NotNull(activeSession);
        Assert.True(activeSession.IsDemoAccount);

        link.Revoke(DateTime.UtcNow);
        await context.SaveChangesAsync();
        Assert.Null(await service.GetSessionAsync($"Bearer {demoLogin.Token}"));
    }

    [Fact]
    public async Task LoginAsync_BillingExemptCompany_AllowsAccessWithoutPaidMonth()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher();
        var user = CreateOwner(hasher, isActive: true);
        user.Company.SetBillingExemption(true, Guid.NewGuid(), DateTime.UtcNow);
        var subscription = new Subscription(
            user.TenantId,
            "ZeroPaper Gestao",
            180m,
            10,
            DateTime.UtcNow.AddMonths(-1),
            SubscriptionStatus.Active);
        context.AddRange(user.Company, user, subscription);
        await context.SaveChangesAsync();

        var service = CreateService(context, hasher, new AcceptingCashOrderTableService());
        var result = await service.LoginAsync(new LoginRequestDto
        {
            Email = user.Email,
            Password = "correct-password"
        });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task LoginAsync_NonExemptCompany_StillRejectsExpiredSubscription()
    {
        await using var context = CreateContext();
        var hasher = new PasswordHasher();
        var user = CreateOwner(hasher, isActive: true);
        var subscription = new Subscription(
            user.TenantId,
            "ZeroPaper Gestao",
            180m,
            10,
            DateTime.UtcNow.AddMonths(-1),
            SubscriptionStatus.Active);
        context.AddRange(user.Company, user, subscription);
        await context.SaveChangesAsync();

        var service = CreateService(context, hasher, new AcceptingCashOrderTableService());

        await Assert.ThrowsAsync<SubscriptionExpiredException>(() => service.LoginAsync(new LoginRequestDto
        {
            Email = user.Email,
            Password = "correct-password"
        }));
    }

    private static ZeroPaperDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ZeroPaperDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ZeroPaperDbContext(options);
    }

    private static AppUser CreateOwner(IPasswordHasher hasher, bool isActive)
    {
        var tenantId = Guid.NewGuid();
        var company = new Company(tenantId, "Pending Company", "Pending Company", $"pending-{Guid.NewGuid():N}");
        company.ChangeBusinessSegment(BusinessSegment.Restaurant);
        var user = new AppUser(
            tenantId,
            company.Id,
            "Pending Owner",
            "pending@example.com",
            hasher.Hash("correct-password"),
            UserRole.Owner);

        if (!isActive) user.Deactivate();
        typeof(AppUser).GetProperty(nameof(AppUser.Company))!.SetValue(user, company);
        return user;
    }

    private static AuthSessionService CreateService(
        ZeroPaperDbContext context,
        IPasswordHasher hasher,
        ICashOrderTableService? cashOrderTableService = null) =>
        new(context, hasher, cashOrderTableService ?? new NoOpCashOrderTableService(), new HttpContextAccessor(), new NoOpPlatformBillingService());

    private static string ComputeTokenHash(string token) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    private sealed class AcceptingCashOrderTableService : ICashOrderTableService
    {
        public Task<DiningTable> EnsureAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<DiningTable>(null!);

        public Task EnsureForActiveOwnersAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpCashOrderTableService : ICashOrderTableService
    {
        public Task<DiningTable> EnsureAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Pending login must not initialize a cash table.");

        public Task EnsureForActiveOwnersAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpPlatformBillingService : IPlatformBillingService
    {
        public Task<DateTime?> RefreshTenantPaidAccessAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.FromResult<DateTime?>(null);
        public Task<AdminPlatformBillingStatusDto> GetStatusAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AdminPlatformBillingStatusDto> ConfigureAsync(WorkspaceSessionContext session, ConfigureAdminPlatformBillingRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DisconnectAsync(WorkspaceSessionContext session, AdminSensitiveActionRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AdminSubscriptionCheckoutDto> CreateSubscriptionCheckoutAsync(WorkspaceSessionContext session, Guid companyId, AdminSensitiveActionRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AdminSubscriptionCheckoutDto> SyncSubscriptionAsync(WorkspaceSessionContext session, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AdminSubscriptionCheckoutDto> CreateSignupCheckoutAsync(Guid subscriptionId, Guid companyId, string ownerEmail, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SubscriptionPaymentConfirmationDto> ConfirmSignupPaymentAsync(string confirmationToken, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SubscriptionPaymentConfirmationDto> MarkManualPaymentAsync(WorkspaceSessionContext session, Guid companyId, AdminSensitiveActionRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
