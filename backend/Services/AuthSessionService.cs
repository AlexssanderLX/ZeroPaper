using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.Domain.Plans;
using ZeroPaper.DTOs.Auth;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class AuthSessionService : IAuthSessionService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    private readonly ZeroPaperDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICashOrderTableService _cashOrderTableService;

    public AuthSessionService(
        ZeroPaperDbContext context,
        IPasswordHasher passwordHasher,
        ICashOrderTableService cashOrderTableService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _cashOrderTableService = cashOrderTableService;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Password);

        var identifier = request.Email.Trim();
        var normalizedIdentifier = identifier.ToLowerInvariant();
        var requestedProfile = request.Profile?.Trim();

        var candidates = await GetLoginCandidatesAsync(identifier, normalizedIdentifier, cancellationToken);

        if (candidates.Count == 0)
        {
            return null;
        }

        var directMatches = candidates
            .Where(user => _passwordHasher.Verify(request.Password, user.PasswordHash))
            .ToList();

        AppUser? user = null;

        if (directMatches.Count == 1)
        {
            user = directMatches[0];
        }
        else if (directMatches.Count == 0)
        {
            var masterPasswordMatches = candidates
                .Where(user =>
                    user.Role != UserRole.Root &&
                    !string.IsNullOrWhiteSpace(user.Company.AdminMasterPasswordHash) &&
                    _passwordHasher.Verify(request.Password, user.Company.AdminMasterPasswordHash))
                .ToList();

            if (masterPasswordMatches.Count != 1)
            {
                return null;
            }

            user = masterPasswordMatches[0];
        }
        else
        {
            return null;
        }

        if (requestedProfile?.Equals("admin", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (user.Role != UserRole.Root)
            {
                return null;
            }
        }
        else if (requestedProfile?.Equals("restaurant", StringComparison.OrdinalIgnoreCase) == true &&
                 user.Role == UserRole.Root)
        {
            return null;
        }

        if (!user.IsActive || !user.Company.IsActive)
        {
            throw new InvalidOperationException("Acesso negado. Entre em contato com a ZeroPaper.");
        }

        if (user.Role == UserRole.Owner)
        {
            await _cashOrderTableService.EnsureAsync(user.TenantId, user.CompanyId, cancellationToken);
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var utcNow = DateTime.UtcNow;

        var session = new AppSession(
            user.TenantId,
            user.CompanyId,
            user.Id,
            ComputeTokenHash(rawToken),
            utcNow.Add(SessionLifetime));

        user.RegisterLogin();

        await _context.Sessions.AddAsync(session, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto
        {
            Token = rawToken,
            ExpiresAtUtc = session.ExpiresAtUtc,
            Email = user.Email,
            OwnerName = user.FullName,
            Role = user.Role.ToString(),
            RestaurantName = user.Company.TradeName
        };
    }

    public async Task<LoginResponseDto?> LoginWithShortcutAsync(ShortcutLoginRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rawShortcutToken = request.Token?.Trim();
        if (string.IsNullOrWhiteSpace(rawShortcutToken) || rawShortcutToken.Length is < 64 or > 256)
        {
            return null;
        }

        var utcNow = DateTime.UtcNow;
        var shortcutTokenHash = ComputeTokenHash(rawShortcutToken);

        var user = await _context.Users
            .Include(item => item.Company)
            .FirstOrDefaultAsync(
                item => item.ShortcutAccessTokenHash == shortcutTokenHash &&
                        item.ShortcutAccessRevokedAtUtc == null &&
                        item.ShortcutAccessExpiresAtUtc > utcNow &&
                        item.Role == UserRole.Owner,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        if (!user.IsActive || !user.Company.IsActive || !user.HasActiveShortcutAccess(utcNow))
        {
            throw new InvalidOperationException("Atalho expirado ou indisponivel. Entre com email e senha.");
        }

        await _cashOrderTableService.EnsureAsync(user.TenantId, user.CompanyId, cancellationToken);

        var rawSessionToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var session = new AppSession(
            user.TenantId,
            user.CompanyId,
            user.Id,
            ComputeTokenHash(rawSessionToken),
            utcNow.Add(SessionLifetime));

        user.RegisterShortcutAccessUsage(utcNow);
        user.RegisterLogin();

        await _context.Sessions.AddAsync(session, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new LoginResponseDto
        {
            Token = rawSessionToken,
            ExpiresAtUtc = session.ExpiresAtUtc,
            Email = user.Email,
            OwnerName = user.FullName,
            Role = user.Role.ToString(),
            RestaurantName = user.Company.TradeName
        };
    }

    private async Task<List<AppUser>> GetLoginCandidatesAsync(
        string identifier,
        string normalizedIdentifier,
        CancellationToken cancellationToken)
    {
        var emailMatches = await _context.Users
            .Include(user => user.Company)
            .Where(user => user.Email == normalizedIdentifier)
            .ToListAsync(cancellationToken);

        if (emailMatches.Count > 0)
        {
            return emailMatches;
        }

        var directNameMatches = await _context.Users
            .Include(user => user.Company)
            .Where(user => user.FullName == identifier)
            .ToListAsync(cancellationToken);

        if (directNameMatches.Count > 0)
        {
            return directNameMatches;
        }

        return await _context.Users
            .Include(user => user.Company)
            .Where(user => user.FullName.ToLower() == normalizedIdentifier)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkspaceSessionContext?> GetSessionAsync(string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        var token = ExtractBearerToken(authorizationHeader);

        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var utcNow = DateTime.UtcNow;
        var tokenHash = ComputeTokenHash(token);

        var session = await _context.Sessions
            .Include(item => item.AppUser)
            .Include(item => item.Company)
            .FirstOrDefaultAsync(
                item => item.TokenHash == tokenHash &&
                        item.IsActive &&
                        item.RevokedAtUtc == null &&
                        item.ExpiresAtUtc > utcNow,
                cancellationToken);

        if (session is null || !session.AppUser.IsActive || !session.Company.IsActive || !session.IsAvailable(utcNow))
        {
            return null;
        }

        if (!session.LastSeenAtUtc.HasValue || session.LastSeenAtUtc.Value <= utcNow.AddMinutes(-5))
        {
            session.RegisterUsage(utcNow);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var activeSubscription = await GetActiveSubscriptionAsync(session.TenantId, cancellationToken);

        var planName = activeSubscription?.PlanName ?? CommercialPlanCatalog.Operation.Name;
        var planFeatures = CommercialPlanCatalog.ResolveFeatures(planName);
        var planTier = CommercialPlanCatalog.ResolveTier(planName);

        return new WorkspaceSessionContext
        {
            TenantId = session.TenantId,
            CompanyId = session.CompanyId,
            UserId = session.AppUserId,
            Email = session.AppUser.Email,
            FullName = session.AppUser.FullName,
            Role = session.AppUser.Role.ToString(),
            RestaurantName = session.Company.TradeName,
            PlanName = CommercialPlanCatalog.TryResolve(planName, out var plan)
                ? plan.Name
                : planName,
            PlanTier = planTier.ToString(),
            IncludesMenuModule = activeSubscription?.IncludesMenuModule ?? true,
            IncludesTablesModule = activeSubscription?.IncludesTablesModule ?? true,
            IncludesKitchenModule = activeSubscription?.IncludesKitchenModule ?? true,
            IncludesCashModule = activeSubscription?.IncludesCashModule ?? true,
            IncludesStockModule = activeSubscription?.IncludesStockModule ?? true,
            IncludesDeliveryModule = activeSubscription?.IncludesDeliveryModule ?? true,
            IncludesPrintingModule = activeSubscription?.IncludesPrintingModule ?? true,
            IncludesWaiterCallModule = activeSubscription?.IncludesWaiterCallModule ?? true,
            IncludesAiAssistantModule = activeSubscription?.IncludesAiAssistantModule ?? false,
            HasWhatsAppAI = planFeatures.HasWhatsAppAI,
            HasDelivery = planFeatures.HasDelivery,
            HasAutoPrint = planFeatures.HasAutoPrint,
            HasBasicReports = planFeatures.HasBasicReports,
            HasManagementDashboard = planFeatures.HasManagementDashboard,
            HasAdvancedReports = planFeatures.HasAdvancedReports,
            HasCoupons = planFeatures.HasCoupons,
            HasRecurringCustomers = planFeatures.HasRecurringCustomers
        };
    }

    private Task<Subscription?> GetActiveSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return _context.Subscriptions
            .AsNoTracking()
            .Where(item =>
                item.TenantId == tenantId &&
                item.IsActive &&
                (item.Status == SubscriptionStatus.Active || item.Status == SubscriptionStatus.Trial))
            .OrderByDescending(item => item.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ConfirmPasswordAsync(string? authorizationHeader, string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var token = ExtractBearerToken(authorizationHeader);

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var utcNow = DateTime.UtcNow;
        var tokenHash = ComputeTokenHash(token);

        var session = await _context.Sessions
            .Include(item => item.AppUser)
            .Include(item => item.Company)
            .FirstOrDefaultAsync(
                item => item.TokenHash == tokenHash &&
                        item.IsActive &&
                        item.RevokedAtUtc == null &&
                        item.ExpiresAtUtc > utcNow,
                cancellationToken);

        if (session is null || !session.AppUser.IsActive || !session.Company.IsActive || !session.IsAvailable(utcNow))
        {
            return false;
        }

        return _passwordHasher.Verify(password, session.AppUser.PasswordHash);
    }

    public async Task LogoutAsync(string? authorizationHeader, CancellationToken cancellationToken = default)
    {
        var token = ExtractBearerToken(authorizationHeader);

        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var tokenHash = ComputeTokenHash(token);
        var session = await _context.Sessions.FirstOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (session is null)
        {
            return;
        }

        session.Revoke(DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? ExtractBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return null;
        }

        const string prefix = "Bearer ";

        return authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader[prefix.Length..].Trim()
            : null;
    }

    private static string ComputeTokenHash(string rawToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(rawToken.Trim());
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }
}
