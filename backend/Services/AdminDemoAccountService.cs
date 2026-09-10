using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Domain.Plans;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class AdminDemoAccountService : IAdminDemoAccountService
{
    private const string DemoTenantIdentifier = "zeropaper-demo-restaurante";
    private readonly ZeroPaperDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICashOrderTableService _cashOrderTableService;
    private readonly PublicAppOptions _publicAppOptions;

    public AdminDemoAccountService(
        ZeroPaperDbContext context,
        IPasswordHasher passwordHasher,
        ICashOrderTableService cashOrderTableService,
        IOptions<PublicAppOptions> publicAppOptions)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _cashOrderTableService = cashOrderTableService;
        _publicAppOptions = publicAppOptions.Value;
    }

    public async Task<AdminDemoAccountDto> GetAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);
        var company = await FindDemoCompanyAsync(cancellationToken);
        return company is null ? new AdminDemoAccountDto() : await MapAsync(company, cancellationToken);
    }

    public async Task<AdminDemoAccountDto> EnsureAccountAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);
        var existing = await FindDemoCompanyAsync(cancellationToken);
        if (existing is not null) return await MapAsync(existing, cancellationToken);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var conflictingTenant = await _context.Tenants.AnyAsync(item => item.Identifier == DemoTenantIdentifier, cancellationToken);
        if (conflictingTenant)
        {
            throw new InvalidOperationException("O identificador reservado para a conta demo ja esta em uso.");
        }

        var tenant = new Tenant("ZeroPaper Demonstracao Restaurante", DemoTenantIdentifier);
        var company = new Company(
            tenant.Id,
            "ZeroPaper Demonstracao Restaurante",
            "Restaurante Demonstracao",
            "restaurante-demonstracao",
            contactEmail: "demo@zeropaper.invalid");
        company.MarkAsDemoAccount();
        company.ChangeBusinessSegment(BusinessSegment.Restaurant);

        var owner = new AppUser(
            tenant.Id,
            company.Id,
            "Visitante demonstracao",
            "demo.restaurante@zeropaper.invalid",
            _passwordHasher.Hash(Convert.ToHexString(RandomNumberGenerator.GetBytes(48))),
            UserRole.Owner);

        var subscription = new Subscription(
            tenant.Id,
            CommercialPlanCatalog.Management.Name,
            0m,
            CommercialPlanCatalog.Management.DefaultMaxUsers,
            DateTime.UtcNow,
            SubscriptionStatus.Active);
        subscription.ApplyCommercialPlan(CommercialPlanCatalog.Management);
        subscription.UpdateFeatureSet(
            includesMenuModule: true,
            includesTablesModule: true,
            includesKitchenModule: true,
            includesCashModule: true,
            includesStockModule: true,
            includesDeliveryModule: true,
            includesPrintingModule: true,
            includesWaiterCallModule: true,
            includesAiAssistantModule: true);

        var qrCodeAccess = new QrCodeAccess(
            tenant.Id,
            company.Id,
            "Cardapio demonstracao",
            "/r/restaurante-demonstracao/menu");

        await _context.Tenants.AddAsync(tenant, cancellationToken);
        await _context.Companies.AddAsync(company, cancellationToken);
        await _context.Users.AddAsync(owner, cancellationToken);
        await _context.Subscriptions.AddAsync(subscription, cancellationToken);
        await _context.QrCodeAccesses.AddAsync(qrCodeAccess, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await _cashOrderTableService.EnsureAsync(tenant.Id, company.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await MapAsync(company, cancellationToken);
    }

    public async Task<AdminDemoLinkCreatedDto> RotateLinkAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);
        var company = await FindDemoCompanyAsync(cancellationToken)
            ?? throw new InvalidOperationException("Crie a conta de demonstracao antes de gerar o link.");
        var owner = await _context.Users.FirstAsync(
            item => item.CompanyId == company.Id && item.Role == UserRole.Owner && item.IsActive,
            cancellationToken);

        var utcNow = DateTime.UtcNow;
        await RevokeCurrentLinksAndSessionsAsync(company.Id, utcNow, cancellationToken);
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var link = new DemoAccessLink(company.TenantId, company.Id, owner.Id, ComputeTokenHash(rawToken), session.UserId);
        await _context.DemoAccessLinks.AddAsync(link, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var status = await MapAsync(company, cancellationToken);
        return new AdminDemoLinkCreatedDto
        {
            Exists = status.Exists,
            CompanyId = status.CompanyId,
            RestaurantName = status.RestaurantName,
            PlanName = status.PlanName,
            HasActiveLink = status.HasActiveLink,
            LinkCreatedAtUtc = status.LinkCreatedAtUtc,
            LinkLastUsedAtUtc = status.LinkLastUsedAtUtc,
            ActiveSessionCount = status.ActiveSessionCount,
            AccessUrl = $"{ResolveFrontendBaseUrl()}/demo#token={rawToken}"
        };
    }

    public async Task<AdminDemoAccountDto> RevokeLinkAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);
        var company = await FindDemoCompanyAsync(cancellationToken)
            ?? throw new InvalidOperationException("Conta de demonstracao nao encontrada.");
        await RevokeCurrentLinksAndSessionsAsync(company.Id, DateTime.UtcNow, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return await MapAsync(company, cancellationToken);
    }

    public async Task<AdminDemoAccountDto> RevokeSessionsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);
        var company = await FindDemoCompanyAsync(cancellationToken)
            ?? throw new InvalidOperationException("Conta de demonstracao nao encontrada.");
        var utcNow = DateTime.UtcNow;
        var sessions = await _context.Sessions
            .Where(item => item.CompanyId == company.Id && item.DemoAccessLinkId != null && item.IsActive && item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var item in sessions) item.Revoke(utcNow);
        await _context.SaveChangesAsync(cancellationToken);
        return await MapAsync(company, cancellationToken);
    }

    private Task<Company?> FindDemoCompanyAsync(CancellationToken cancellationToken) =>
        _context.Companies.FirstOrDefaultAsync(item => item.IsDemoAccount && item.IsActive, cancellationToken);

    private async Task RevokeCurrentLinksAndSessionsAsync(Guid companyId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var links = await _context.DemoAccessLinks
            .Where(item => item.CompanyId == companyId && item.IsActive && item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var item in links) item.Revoke(utcNow);

        var sessions = await _context.Sessions
            .Where(item => item.CompanyId == companyId && item.DemoAccessLinkId != null && item.IsActive && item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var item in sessions) item.Revoke(utcNow);
    }

    private async Task<AdminDemoAccountDto> MapAsync(Company company, CancellationToken cancellationToken)
    {
        var activeLink = await _context.DemoAccessLinks.AsNoTracking()
            .Where(item => item.CompanyId == company.Id && item.IsActive && item.RevokedAtUtc == null)
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var activeSessions = await _context.Sessions.CountAsync(
            item => item.CompanyId == company.Id && item.DemoAccessLinkId != null && item.IsActive &&
                    item.RevokedAtUtc == null && item.ExpiresAtUtc > DateTime.UtcNow,
            cancellationToken);
        var planName = await _context.Subscriptions.AsNoTracking()
            .Where(item => item.TenantId == company.TenantId && item.IsActive)
            .OrderByDescending(item => item.StartsAtUtc)
            .Select(item => item.PlanName)
            .FirstOrDefaultAsync(cancellationToken) ?? CommercialPlanCatalog.Management.Name;

        return new AdminDemoAccountDto
        {
            Exists = true,
            CompanyId = company.Id,
            RestaurantName = company.TradeName,
            PlanName = planName,
            HasActiveLink = activeLink is not null,
            LinkCreatedAtUtc = activeLink?.CreatedAtUtc,
            LinkLastUsedAtUtc = activeLink?.LastUsedAtUtc,
            ActiveSessionCount = activeSessions
        };
    }

    private string ResolveFrontendBaseUrl()
    {
        var configured = Environment.GetEnvironmentVariable("PUBLIC_APP_BASE_URL") ?? _publicAppOptions.BaseUrl;
        return string.IsNullOrWhiteSpace(configured) ? "http://localhost:3000" : configured.Trim().TrimEnd('/');
    }

    private static string ComputeTokenHash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken.Trim())));

    private static void EnsureRoot(WorkspaceSessionContext session)
    {
        if (!session.Role.Equals(UserRole.Root.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Apenas o root pode controlar a conta de demonstracao.");
    }
}
