using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public sealed class CustomerProfileService : ICustomerProfileService
{
    private readonly ZeroPaperDbContext _context;

    public CustomerProfileService(ZeroPaperDbContext context) => _context = context;

    public async Task<CustomerProfileListDto> SearchAsync(
        WorkspaceSessionContext session,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        BusinessCapabilityGuard.Require(session.Capabilities.HasCustomerProfiles, "CustomerProfiles");
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.DeliveryCustomerProfiles.AsNoTracking().Where(item =>
            item.TenantId == session.TenantId && item.CompanyId == session.CompanyId && item.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            var digits = new string(normalized.Where(char.IsDigit).ToArray());
            query = query.Where(item =>
                (item.CustomerName != null && item.CustomerName.Contains(normalized)) ||
                (digits.Length > 0 && item.Phone.Contains(digits)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(item => item.CustomerName).ThenBy(item => item.Phone)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new CustomerProfileListDto { Items = items.Select(Map).ToList(), Page = page, PageSize = pageSize, Total = total };
    }

    public async Task<CustomerProfileDto> GetByIdAsync(WorkspaceSessionContext session, Guid customerId, CancellationToken cancellationToken = default)
    {
        BusinessCapabilityGuard.Require(session.Capabilities.HasCustomerProfiles, "CustomerProfiles");
        return Map(await GetEntityAsync(session, customerId, tracking: false, cancellationToken));
    }

    public async Task<CustomerProfileDto> CreateAsync(WorkspaceSessionContext session, CreateCustomerProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        BusinessCapabilityGuard.Require(session.Capabilities.HasCustomerProfiles, "CustomerProfiles");
        ArgumentNullException.ThrowIfNull(request);
        var phone = DeliveryCustomerProfile.NormalizePhone(request.PhoneNumber);

        if (await _context.DeliveryCustomerProfiles.AnyAsync(item => item.CompanyId == session.CompanyId && item.Phone == phone, cancellationToken))
            throw new InvalidOperationException("Ja existe um cliente com este telefone nesta empresa.");

        var profile = new DeliveryCustomerProfile(
            session.TenantId, session.CompanyId, phone, request.Name, request.Street, request.Number,
            request.Neighborhood, request.Complement, request.ZipCode, DateTime.UtcNow);
        await _context.DeliveryCustomerProfiles.AddAsync(profile, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(profile);
    }

    public async Task<CustomerProfileDto> UpdateAsync(WorkspaceSessionContext session, Guid customerId, UpdateCustomerProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        BusinessCapabilityGuard.Require(session.Capabilities.HasCustomerProfiles, "CustomerProfiles");
        var profile = await GetEntityAsync(session, customerId, tracking: true, cancellationToken);
        profile.UpdateOwnerData(request.Name, request.Street, request.Number, request.Neighborhood, request.Complement, request.ZipCode);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(profile);
    }

    private async Task<DeliveryCustomerProfile> GetEntityAsync(WorkspaceSessionContext session, Guid id, bool tracking, CancellationToken cancellationToken)
    {
        var query = _context.DeliveryCustomerProfiles.AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(item =>
            item.Id == id && item.TenantId == session.TenantId && item.CompanyId == session.CompanyId && item.IsActive,
            cancellationToken) ?? throw new KeyNotFoundException("Cliente nao encontrado.");
    }

    private static CustomerProfileDto Map(DeliveryCustomerProfile profile) => new()
    {
        Id = profile.Id,
        PhoneNumber = profile.Phone,
        Name = profile.CustomerName,
        ZipCode = profile.DeliveryPostalCode,
        Street = profile.DeliveryAddress,
        Number = profile.DeliveryNumber,
        Neighborhood = profile.DeliveryNeighborhood,
        Complement = profile.DeliveryComplement,
        CreatedAtUtc = profile.CreatedAtUtc,
        UpdatedAtUtc = profile.UpdatedAtUtc,
        LastOrderAtUtc = profile.LastOrderAtUtc
    };
}
