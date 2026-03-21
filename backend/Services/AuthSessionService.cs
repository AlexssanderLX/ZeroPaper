using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Auth;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class AuthSessionService : IAuthSessionService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    private readonly ZeroPaperDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public AuthSessionService(ZeroPaperDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
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

        var matches = candidates
            .Where(user => _passwordHasher.Verify(request.Password, user.PasswordHash))
            .ToList();

        if (matches.Count != 1)
        {
            return null;
        }

        var user = matches[0];

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

        return new WorkspaceSessionContext
        {
            TenantId = session.TenantId,
            CompanyId = session.CompanyId,
            UserId = session.AppUserId,
            Email = session.AppUser.Email,
            FullName = session.AppUser.FullName,
            Role = session.AppUser.Role.ToString(),
            RestaurantName = session.Company.TradeName
        };
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
