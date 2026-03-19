using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class AdminUserService : IAdminUserService
{
    private readonly ZeroPaperDbContext _context;

    public AdminUserService(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);

        var utcNow = DateTime.UtcNow;
        var onlineThreshold = utcNow.AddMinutes(-10);

        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(item => item.Role == UserRole.Root ? 0 : 1)
            .ThenBy(item => item.Company.TradeName)
            .ThenBy(item => item.FullName)
            .Select(item => new AdminUserDto
            {
                Id = item.Id,
                FullName = item.FullName,
                Email = item.Email,
                Role = item.Role.ToString(),
                RestaurantName = item.Company.TradeName,
                IsActive = item.IsActive,
                IsCompanyActive = item.Company.IsActive,
                ActiveSessionCount = item.Sessions.Count(sessionItem =>
                    sessionItem.IsActive &&
                    sessionItem.RevokedAtUtc == null &&
                    sessionItem.ExpiresAtUtc > utcNow),
                HasActiveSession = item.Sessions.Any(sessionItem =>
                    sessionItem.IsActive &&
                    sessionItem.RevokedAtUtc == null &&
                    sessionItem.ExpiresAtUtc > utcNow),
                IsOnlineNow = item.Sessions.Any(sessionItem =>
                    sessionItem.IsActive &&
                    sessionItem.RevokedAtUtc == null &&
                    sessionItem.ExpiresAtUtc > utcNow &&
                    (sessionItem.LastSeenAtUtc ?? sessionItem.CreatedAtUtc) >= onlineThreshold),
                LastLoginAtUtc = item.LastLoginAtUtc,
                LastSeenAtUtc = item.Sessions
                    .Where(sessionItem =>
                        sessionItem.IsActive &&
                        sessionItem.RevokedAtUtc == null &&
                        sessionItem.ExpiresAtUtc > utcNow)
                    .OrderByDescending(sessionItem => sessionItem.LastSeenAtUtc ?? sessionItem.CreatedAtUtc)
                    .Select(sessionItem => (DateTime?)(sessionItem.LastSeenAtUtc ?? sessionItem.CreatedAtUtc))
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return users;
    }

    public async Task<AdminUserDto> DeactivateUserAsync(WorkspaceSessionContext session, Guid userId, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);

        var user = await _context.Users
            .Include(item => item.Company)
            .Include(item => item.Sessions)
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Conta nao encontrada.");

        EnsureManagedUser(user);

        user.Deactivate();

        var utcNow = DateTime.UtcNow;
        foreach (var appSession in user.Sessions.Where(item => item.IsAvailable(utcNow)))
        {
            appSession.Revoke(utcNow);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task<AdminUserDto> ReactivateUserAsync(WorkspaceSessionContext session, Guid userId, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);

        var user = await _context.Users
            .Include(item => item.Company)
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Conta nao encontrada.");

        EnsureManagedUser(user);

        user.Activate();
        await _context.SaveChangesAsync(cancellationToken);

        return await GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task DeleteUserAsync(WorkspaceSessionContext session, Guid userId, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);

        var user = await _context.Users
            .Include(item => item.Sessions)
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Conta nao encontrada.");

        EnsureManagedUser(user);

        if (user.Sessions.Count != 0)
        {
            _context.Sessions.RemoveRange(user.Sessions);
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<AdminUserDto> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var onlineThreshold = utcNow.AddMinutes(-10);

        var user = await _context.Users
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new AdminUserDto
            {
                Id = item.Id,
                FullName = item.FullName,
                Email = item.Email,
                Role = item.Role.ToString(),
                RestaurantName = item.Company.TradeName,
                IsActive = item.IsActive,
                IsCompanyActive = item.Company.IsActive,
                ActiveSessionCount = item.Sessions.Count(sessionItem =>
                    sessionItem.IsActive &&
                    sessionItem.RevokedAtUtc == null &&
                    sessionItem.ExpiresAtUtc > utcNow),
                HasActiveSession = item.Sessions.Any(sessionItem =>
                    sessionItem.IsActive &&
                    sessionItem.RevokedAtUtc == null &&
                    sessionItem.ExpiresAtUtc > utcNow),
                IsOnlineNow = item.Sessions.Any(sessionItem =>
                    sessionItem.IsActive &&
                    sessionItem.RevokedAtUtc == null &&
                    sessionItem.ExpiresAtUtc > utcNow &&
                    (sessionItem.LastSeenAtUtc ?? sessionItem.CreatedAtUtc) >= onlineThreshold),
                LastLoginAtUtc = item.LastLoginAtUtc,
                LastSeenAtUtc = item.Sessions
                    .Where(sessionItem =>
                        sessionItem.IsActive &&
                        sessionItem.RevokedAtUtc == null &&
                        sessionItem.ExpiresAtUtc > utcNow)
                    .OrderByDescending(sessionItem => sessionItem.LastSeenAtUtc ?? sessionItem.CreatedAtUtc)
                    .Select(sessionItem => (DateTime?)(sessionItem.LastSeenAtUtc ?? sessionItem.CreatedAtUtc))
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return user ?? throw new KeyNotFoundException("Conta nao encontrada.");
    }

    private static void EnsureRoot(WorkspaceSessionContext session)
    {
        if (!Enum.TryParse<UserRole>(session.Role, true, out var role) || role != UserRole.Root)
        {
            throw new UnauthorizedAccessException("Root access is required.");
        }
    }

    private static void EnsureManagedUser(Domain.Entities.AppUser user)
    {
        if (user.Role == UserRole.Root)
        {
            throw new InvalidOperationException("A conta root nao pode ser alterada por esta acao.");
        }
    }
}
