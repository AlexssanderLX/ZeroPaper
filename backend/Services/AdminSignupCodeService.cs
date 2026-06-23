using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class AdminSignupCodeService : IAdminSignupCodeService
{
    private readonly ZeroPaperDbContext _context;

    public AdminSignupCodeService(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SignupCodeDto>> GetSignupCodesAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);

        var codes = await _context.SignupCodes
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        return codes.Select(Map).ToList();
    }

    public async Task<CreateSignupCodeResponseDto> CreateSignupCodeAsync(WorkspaceSessionContext session, CreateSignupCodeRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);
        ArgumentNullException.ThrowIfNull(request);

        var rawCode = SignupCode.GenerateRawCode();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(5);

        var code = new SignupCode(
            request.Label,
            rawCode,
            expiresAtUtc,
            1,
            session.UserId,
            request.BoundEmail,
            request.AllowedPlanName,
            request.AllowedMaxUsers);

        await _context.SignupCodes.AddAsync(code, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateSignupCodeResponseDto
        {
            Id = code.Id,
            Label = code.Label,
            BoundEmail = code.BoundEmail,
            AllowedPlanName = code.AllowedPlanName,
            AllowedMaxUsers = code.AllowedMaxUsers,
            ExpiresAtUtc = code.ExpiresAtUtc,
            MaxUses = code.MaxUses,
            UsedCount = code.UsedCount,
            IsActive = code.IsActive,
            CreatedAtUtc = code.CreatedAtUtc,
            LastUsedAtUtc = code.LastUsedAtUtc,
            RawCode = rawCode
        };
    }

    public async Task DeleteSignupCodeAsync(WorkspaceSessionContext session, Guid codeId, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);

        var code = await _context.SignupCodes
            .FirstOrDefaultAsync(item => item.Id == codeId, cancellationToken)
            ?? throw new KeyNotFoundException("Codigo nao encontrado.");

        _context.SignupCodes.Remove(code);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CleanupSignupCodesResponseDto> CleanupSignupCodesAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);

        var utcNow = DateTime.UtcNow;
        var removableCodes = await _context.SignupCodes
            .Where(item =>
                item.ExpiresAtUtc <= utcNow ||
                !item.IsActive ||
                item.UsedCount >= item.MaxUses)
            .ToListAsync(cancellationToken);

        if (removableCodes.Count > 0)
        {
            _context.SignupCodes.RemoveRange(removableCodes);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var remainingCount = await _context.SignupCodes.CountAsync(cancellationToken);

        return new CleanupSignupCodesResponseDto
        {
            DeletedCount = removableCodes.Count,
            RemainingCount = remainingCount
        };
    }

    private static void EnsureRoot(WorkspaceSessionContext session)
    {
        if (!Enum.TryParse<UserRole>(session.Role, true, out var role) || role != UserRole.Root)
        {
            throw new UnauthorizedAccessException("Root access is required.");
        }
    }

    private static SignupCodeDto Map(SignupCode code)
    {
        return new SignupCodeDto
        {
            Id = code.Id,
            Label = code.Label,
            BoundEmail = code.BoundEmail,
            AllowedPlanName = code.AllowedPlanName,
            AllowedMaxUsers = code.AllowedMaxUsers,
            ExpiresAtUtc = code.ExpiresAtUtc,
            MaxUses = code.MaxUses,
            UsedCount = code.UsedCount,
            IsActive = code.IsActive,
            CreatedAtUtc = code.CreatedAtUtc,
            LastUsedAtUtc = code.LastUsedAtUtc
        };
    }
}
