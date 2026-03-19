using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Services;

public class PlatformRootSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PlatformRootSeeder> _logger;

    public PlatformRootSeeder(IServiceProvider serviceProvider, ILogger<PlatformRootSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task EnsureRootAccountAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var context = scope.ServiceProvider.GetRequiredService<ZeroPaperDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var email = configuration["RootAccount:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["RootAccount:Password"]?.Trim();
        var fullName = configuration["RootAccount:Name"]?.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("Root account seed skipped because RootAccount configuration was not provided.");
            return;
        }

        var tenant = await context.Tenants.FirstOrDefaultAsync(item => item.Identifier == "zeropaper-platform", cancellationToken);
        if (tenant is null)
        {
            tenant = new Tenant("ZeroPaper Platform", "zeropaper-platform");
            await context.Tenants.AddAsync(tenant, cancellationToken);
        }

        var company = await context.Companies.FirstOrDefaultAsync(
            item => item.TenantId == tenant.Id && item.AccessSlug == "platform-admin",
            cancellationToken);

        if (company is null)
        {
            company = new Company(
                tenant.Id,
                "ZeroPaper Platform LTDA",
                "ZeroPaper Platform",
                "platform-admin",
                contactEmail: email);

            await context.Companies.AddAsync(company, cancellationToken);
        }
        else
        {
            company.UpdateNames("ZeroPaper Platform LTDA", "ZeroPaper Platform");
            company.ChangeAccessSlug("platform-admin");
            company.UpdateContact(email, company.ContactPhone);
            company.Activate();
        }

        var user = await context.Users.FirstOrDefaultAsync(item => item.Email == email, cancellationToken);

        if (user is null)
        {
            user = new AppUser(
                tenant.Id,
                company.Id,
                string.IsNullOrWhiteSpace(fullName) ? "ZeroPaper Root" : fullName,
                email,
                passwordHasher.Hash(password),
                UserRole.Root);

            await context.Users.AddAsync(user, cancellationToken);
        }
        else
        {
            user.ChangeIdentity(string.IsNullOrWhiteSpace(fullName) ? user.FullName : fullName, email);
            user.ChangeRole(UserRole.Root);
            user.ChangePasswordHash(passwordHasher.Hash(password));
            user.Activate();
        }

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Root account ensured for {Email}.", email);
    }
}
