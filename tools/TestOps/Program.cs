using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Onboarding;
using ZeroPaper.Repositories;
using ZeroPaper.Services;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings__DefaultConnection was not provided.");
}

var options = new DbContextOptionsBuilder<ZeroPaperDbContext>()
    .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
    .Options;

await using var context = new ZeroPaperDbContext(options);

var command = args.FirstOrDefault()?.Trim().ToLowerInvariant();

switch (command)
{
    case "cleanup-tests":
        await CleanupTestsAsync(context);
        break;
    case "list-users":
        await ListUsersAsync(context);
        break;
    case "list-signup-codes":
        await ListSignupCodesAsync(context);
        break;
    case "probe-onboarding":
        await ProbeOnboardingAsync(context, args.Skip(1).ToArray());
        break;
    default:
        Console.WriteLine("Commands: cleanup-tests | list-users | list-signup-codes | probe-onboarding");
        break;
}

static async Task ListUsersAsync(ZeroPaperDbContext context)
{
    var users = await context.Users
        .Include(user => user.Company)
        .OrderBy(user => user.Company.TradeName)
        .ThenBy(user => user.Email)
        .Select(user => new
        {
            user.Email,
            user.FullName,
            Role = user.Role.ToString(),
            Restaurant = user.Company.TradeName
        })
        .ToListAsync();

    foreach (var user in users)
    {
        Console.WriteLine($"{user.Restaurant} | {user.Role} | {user.Email} | {user.FullName}");
    }
}

static async Task CleanupTestsAsync(ZeroPaperDbContext context)
{
    var rootUserIds = await context.Users
        .Where(user => user.Role == UserRole.Root)
        .Select(user => user.Id)
        .ToListAsync();

    var rootCompanyIds = await context.Users
        .Where(user => user.Role == UserRole.Root)
        .Select(user => user.CompanyId)
        .Distinct()
        .ToListAsync();

    var rootTenantIds = await context.Users
        .Where(user => user.Role == UserRole.Root)
        .Select(user => user.TenantId)
        .Distinct()
        .ToListAsync();

    var passwordResetRequests = await context.PasswordResetRequests
        .Where(item => !rootUserIds.Contains(item.AppUserId))
        .ToListAsync();

    var sessions = await context.Sessions
        .Where(item => !rootUserIds.Contains(item.AppUserId))
        .ToListAsync();

    var users = await context.Users
        .Where(user => user.Role != UserRole.Root)
        .ToListAsync();

    var orderItems = await context.OrderItems.ToListAsync();
    var orders = await context.CustomerOrders
        .Where(item => !rootCompanyIds.Contains(item.CompanyId))
        .ToListAsync();

    var tables = await context.DiningTables
        .Where(item => !rootCompanyIds.Contains(item.CompanyId))
        .ToListAsync();

    var stockItems = await context.StockItems
        .Where(item => !rootCompanyIds.Contains(item.CompanyId))
        .ToListAsync();

    var qrCodes = await context.QrCodeAccesses
        .Where(item => !rootCompanyIds.Contains(item.CompanyId))
        .ToListAsync();

    var subscriptions = await context.Subscriptions
        .Where(item => !rootTenantIds.Contains(item.TenantId))
        .ToListAsync();

    var companies = await context.Companies
        .Where(item => !rootTenantIds.Contains(item.TenantId))
        .ToListAsync();

    var tenants = await context.Tenants
        .Where(item => !rootTenantIds.Contains(item.Id))
        .ToListAsync();

    var signupCodes = await context.SignupCodes.ToListAsync();

    context.PasswordResetRequests.RemoveRange(passwordResetRequests);
    context.Sessions.RemoveRange(sessions);
    context.OrderItems.RemoveRange(orderItems);
    context.CustomerOrders.RemoveRange(orders);
    context.DiningTables.RemoveRange(tables);
    context.StockItems.RemoveRange(stockItems);
    context.QrCodeAccesses.RemoveRange(qrCodes);
    context.Subscriptions.RemoveRange(subscriptions);
    context.Users.RemoveRange(users);
    context.Companies.RemoveRange(companies);
    context.Tenants.RemoveRange(tenants);
    context.SignupCodes.RemoveRange(signupCodes);

    await context.SaveChangesAsync();

    Console.WriteLine($"Removed non-root test data. Users: {users.Count}, Companies: {companies.Count}, Tenants: {tenants.Count}, SignupCodes: {signupCodes.Count}");
}

static async Task ListSignupCodesAsync(ZeroPaperDbContext context)
{
    var signupCodes = await context.SignupCodes
        .OrderByDescending(item => item.CreatedAtUtc)
        .Select(item => new
        {
            item.Label,
            item.BoundEmail,
            item.IsActive,
            item.UsedCount,
            item.MaxUses,
            item.ExpiresAtUtc
        })
        .ToListAsync();

    foreach (var signupCode in signupCodes)
    {
        Console.WriteLine($"{signupCode.Label} | {signupCode.BoundEmail ?? "sem-email"} | ativo={signupCode.IsActive} | usos={signupCode.UsedCount}/{signupCode.MaxUses} | expira={signupCode.ExpiresAtUtc:O}");
    }
}

static async Task ProbeOnboardingAsync(ZeroPaperDbContext context, string[] args)
{
    if (args.Length < 2)
    {
        throw new InvalidOperationException("Usage: probe-onboarding <ownerEmail> <accessCode>");
    }

    var service = new RestaurantOnboardingService(
        context,
        new TenantRepository(context),
        new CompanyRepository(context),
        new AppUserRepository(context),
        new SubscriptionRepository(context),
        new QrCodeAccessRepository(context),
        new UnitOfWork(context),
        new PasswordHasher());

    var suffix = args[0].Split("@")[0].Split(".").Last();
    var request = new RestaurantOnboardingRequestDto
    {
        RestaurantName = $"Restaurante {suffix}",
        LegalName = $"Restaurante {suffix} LTDA",
        OwnerName = "Dono Novo",
        OwnerEmail = args[0],
        AccessCode = args[1],
        OwnerPassword = "Teste123!",
        ContactPhone = "(11) 99999-0000",
        PlanName = "ZeroPaper Base",
        MonthlyPrice = 0,
        MaxUsers = 1
    };

    try
    {
        var response = await service.CreateAsync(request);
        Console.WriteLine($"OK | {response.OwnerEmail} | {response.AccessSlug}");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"{exception.GetType().Name}: {exception.Message}");
        Console.WriteLine(exception.ToString());
    }
}
