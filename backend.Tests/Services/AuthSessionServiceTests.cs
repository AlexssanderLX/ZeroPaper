using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Auth;
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

    private static AuthSessionService CreateService(ZeroPaperDbContext context, IPasswordHasher hasher) =>
        new(context, hasher, new NoOpCashOrderTableService(), new HttpContextAccessor());

    private sealed class NoOpCashOrderTableService : ICashOrderTableService
    {
        public Task<DiningTable> EnsureAsync(Guid tenantId, Guid companyId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Pending login must not initialize a cash table.");

        public Task EnsureForActiveOwnersAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
