using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class AdminOwnerService : IAdminOwnerService
{
    private const string HardDeleteConfirmationText = "EXCLUIR OWNER";

    private readonly ZeroPaperDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICashOrderTableService _cashOrderTableService;

    public AdminOwnerService(
        ZeroPaperDbContext context,
        IPasswordHasher passwordHasher,
        ICashOrderTableService cashOrderTableService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _cashOrderTableService = cashOrderTableService;
    }

    public async Task<IReadOnlyList<AdminOwnerDto>> GetOwnersAsync(
        WorkspaceSessionContext session,
        CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);

        var utcNow = DateTime.UtcNow;
        return await _context.Users
            .AsNoTracking()
            .Where(item => item.Role == UserRole.Owner && item.Company.IsActive)
            .OrderBy(item => item.Company.TradeName)
            .ThenBy(item => item.FullName)
            .Select(item => new AdminOwnerDto
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                CompanyName = item.Company.TradeName,
                AccessSlug = item.Company.AccessSlug,
                FullName = item.FullName,
                Email = item.Email,
                ContactPhone = item.Company.ContactPhone,
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
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc,
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
    }

    public async Task<AdminOwnerDto?> GetOwnerByIdAsync(
        WorkspaceSessionContext session,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);
        return await GetOwnerProjectionAsync(ownerId, cancellationToken);
    }

    public async Task<AdminOwnerDto> CreateOwnerAsync(
        WorkspaceSessionContext session,
        CreateAdminOwnerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRoot(session);
        await ValidateRootPasswordAsync(session, request.RootPassword, cancellationToken);
        ValidateOwnerPassword(request.OwnerPassword);

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == request.CompanyId, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        var normalizedEmail = NormalizeEmail(request.Email);
        await EnsureEmailAvailableAsync(company.TenantId, normalizedEmail, null, cancellationToken);

        var owner = new AppUser(
            company.TenantId,
            company.Id,
            NormalizeName(request.FullName),
            normalizedEmail,
            _passwordHasher.Hash(request.OwnerPassword),
            UserRole.Owner);

        await _context.Users.AddAsync(owner, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await _cashOrderTableService.EnsureAsync(owner.TenantId, owner.CompanyId, cancellationToken);

        return await GetOwnerProjectionAsync(owner.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Owner criado, mas nao foi possivel carregar o registro.");
    }

    public async Task<AdminOwnerDto> UpdateOwnerAsync(
        WorkspaceSessionContext session,
        Guid ownerId,
        UpdateAdminOwnerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRoot(session);
        await ValidateRootPasswordAsync(session, request.RootPassword, cancellationToken);

        var owner = await GetManagedOwnerAsync(ownerId, cancellationToken);
        var normalizedEmail = NormalizeEmail(request.Email);

        await EnsureEmailAvailableAsync(owner.TenantId, normalizedEmail, owner.Id, cancellationToken);

        owner.ChangeIdentity(NormalizeName(request.FullName), normalizedEmail);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetOwnerProjectionAsync(owner.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Owner nao encontrado.");
    }

    public async Task ResetOwnerPasswordAsync(
        WorkspaceSessionContext session,
        Guid ownerId,
        ResetAdminOwnerPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRoot(session);
        await ValidateRootPasswordAsync(session, request.RootPassword, cancellationToken);
        ValidateOwnerPassword(request.NewPassword);

        var owner = await GetManagedOwnerWithSessionsAsync(ownerId, cancellationToken);
        owner.ChangePasswordHash(_passwordHasher.Hash(request.NewPassword));
        RevokeAvailableSessions(owner, DateTime.UtcNow);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminOwnerDto> DeactivateOwnerAsync(
        WorkspaceSessionContext session,
        Guid ownerId,
        ChangeAdminOwnerStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRoot(session);
        await ValidateRootPasswordAsync(session, request.RootPassword, cancellationToken);

        var owner = await GetManagedOwnerWithSessionsAsync(ownerId, cancellationToken);
        await EnsureAnotherActiveOwnerExistsAsync(owner, cancellationToken);

        owner.Deactivate();
        RevokeAvailableSessions(owner, DateTime.UtcNow);

        await _context.SaveChangesAsync(cancellationToken);

        return await GetOwnerProjectionAsync(owner.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Owner nao encontrado.");
    }

    public async Task<AdminOwnerDto> ReactivateOwnerAsync(
        WorkspaceSessionContext session,
        Guid ownerId,
        ChangeAdminOwnerStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRoot(session);
        await ValidateRootPasswordAsync(session, request.RootPassword, cancellationToken);

        var owner = await GetManagedOwnerAsync(ownerId, cancellationToken);
        owner.Activate();

        await _context.SaveChangesAsync(cancellationToken);
        await _cashOrderTableService.EnsureAsync(owner.TenantId, owner.CompanyId, cancellationToken);

        return await GetOwnerProjectionAsync(owner.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Owner nao encontrado.");
    }

    public async Task HardDeleteOwnerAsync(
        WorkspaceSessionContext session,
        Guid ownerId,
        HardDeleteAdminOwnerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRoot(session);
        await ValidateRootPasswordAsync(session, request.RootPassword, cancellationToken);

        if (!string.Equals(request.ConfirmationText?.Trim(), HardDeleteConfirmationText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Digite {HardDeleteConfirmationText} para confirmar a exclusao definitiva.");
        }

        var owner = await GetManagedOwnerWithSessionsAsync(ownerId, cancellationToken);
        await EnsureAnotherOwnerExistsAsync(owner, cancellationToken);

        if (owner.Sessions.Count != 0)
        {
            _context.Sessions.RemoveRange(owner.Sessions);
        }

        _context.Users.Remove(owner);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private Task<AdminOwnerDto?> GetOwnerProjectionAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        return _context.Users
            .AsNoTracking()
            .Where(item => item.Id == ownerId && item.Role == UserRole.Owner)
            .Select(item => new AdminOwnerDto
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                CompanyName = item.Company.TradeName,
                AccessSlug = item.Company.AccessSlug,
                FullName = item.FullName,
                Email = item.Email,
                ContactPhone = item.Company.ContactPhone,
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
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc,
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
    }

    private async Task<AppUser> GetManagedOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        return await _context.Users
            .Include(item => item.Company)
            .FirstOrDefaultAsync(item => item.Id == ownerId && item.Role == UserRole.Owner, cancellationToken)
            ?? throw new KeyNotFoundException("Owner nao encontrado.");
    }

    private async Task<AppUser> GetManagedOwnerWithSessionsAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        return await _context.Users
            .Include(item => item.Company)
            .Include(item => item.Sessions)
            .FirstOrDefaultAsync(item => item.Id == ownerId && item.Role == UserRole.Owner, cancellationToken)
            ?? throw new KeyNotFoundException("Owner nao encontrado.");
    }

    private static void EnsureRoot(WorkspaceSessionContext session)
    {
        if (!Enum.TryParse<UserRole>(session.Role, true, out var role) || role != UserRole.Root)
        {
            throw new UnauthorizedAccessException("Root access is required.");
        }
    }

    private async Task ValidateRootPasswordAsync(
        WorkspaceSessionContext session,
        string password,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var currentUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == session.UserId &&
                        item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Conta root nao encontrada.");

        if (currentUser.Role != UserRole.Root || !_passwordHasher.Verify(password, currentUser.PasswordHash))
        {
            throw new InvalidOperationException("Senha root incorreta.");
        }
    }

    private async Task EnsureEmailAvailableAsync(
        Guid tenantId,
        string email,
        Guid? ignoredUserId,
        CancellationToken cancellationToken)
    {
        var exists = await _context.Users.AnyAsync(
            item =>
                item.TenantId == tenantId &&
                item.Email == email &&
                (!ignoredUserId.HasValue || item.Id != ignoredUserId.Value),
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Ja existe uma conta com esse email nesta unidade.");
        }
    }

    private async Task EnsureAnotherActiveOwnerExistsAsync(AppUser owner, CancellationToken cancellationToken)
    {
        var hasAnotherActiveOwner = await _context.Users.AnyAsync(
            item =>
                item.CompanyId == owner.CompanyId &&
                item.Role == UserRole.Owner &&
                item.IsActive &&
                item.Id != owner.Id,
            cancellationToken);

        if (!hasAnotherActiveOwner)
        {
            throw new InvalidOperationException("Nao e possivel desativar o unico owner ativo desta unidade. Cadastre ou reative outro owner antes de bloquear este acesso.");
        }
    }

    private async Task EnsureAnotherOwnerExistsAsync(AppUser owner, CancellationToken cancellationToken)
    {
        var hasAnotherOwner = await _context.Users.AnyAsync(
            item =>
                item.CompanyId == owner.CompanyId &&
                item.Role == UserRole.Owner &&
                item.Id != owner.Id,
            cancellationToken);

        if (!hasAnotherOwner)
        {
            throw new InvalidOperationException("Nao e possivel excluir o unico owner desta unidade. Cadastre outro owner primeiro ou desative a unidade inteira se ela nao sera mais usada.");
        }
    }

    private static string NormalizeName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();

        if (normalized.Length > 150)
        {
            throw new ArgumentException("O nome do owner precisa ter no maximo 150 caracteres.", nameof(value));
        }

        return normalized;
    }

    private static string NormalizeEmail(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > 180 || !normalized.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Informe um email valido para o owner.", nameof(value));
        }

        return normalized;
    }

    private static void ValidateOwnerPassword(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Trim().Length < 8)
        {
            throw new ArgumentException("A senha do owner precisa ter pelo menos 8 caracteres.", nameof(value));
        }

        if (value.Trim().Length > 100)
        {
            throw new ArgumentException("A senha do owner precisa ter no maximo 100 caracteres.", nameof(value));
        }
    }

    private static void RevokeAvailableSessions(AppUser owner, DateTime utcNow)
    {
        foreach (var appSession in owner.Sessions.Where(item => item.IsAvailable(utcNow)))
        {
            appSession.Revoke(utcNow);
        }
    }
}
