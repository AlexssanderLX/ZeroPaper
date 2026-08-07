using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;
using ZeroPaper.Security;

namespace ZeroPaper.Services;

public sealed class PublicPetShopService : IPublicPetShopService
{
    private readonly ZeroPaperDbContext _context;
    private readonly IAppointmentService _appointments;

    public PublicPetShopService(ZeroPaperDbContext context, IAppointmentService appointments)
    {
        _context = context;
        _appointments = appointments;
    }

    public async Task<PublicPetShopDto> GetAsync(string publicCode, CancellationToken cancellationToken = default)
    {
        var company = await GetCompanyAsync(publicCode, cancellationToken);
        return new PublicPetShopDto { BusinessName = company.TradeName, LogoUrl = company.LogoUrl, TimeZone = company.TimeZoneId };
    }

    public async Task<IReadOnlyList<PublicPetShopServiceDto>> GetServicesAsync(string publicCode, CancellationToken cancellationToken = default)
    {
        var company = await GetCompanyAsync(publicCode, cancellationToken);
        return await _context.MenuItems.AsNoTracking().Where(item => item.CompanyId == company.Id && item.TenantId == company.TenantId && item.IsActive && item.Kind == CatalogItemKind.Service && item.EstimatedDurationMinutes.HasValue)
            .OrderBy(item => item.DisplayOrder).Select(item => new PublicPetShopServiceDto
            { Id = item.Id, Name = item.Name, Description = item.Description, Price = item.Price, DurationMinutes = item.EstimatedDurationMinutes!.Value, ImageUrl = item.ImageUrl }).ToListAsync(cancellationToken);
    }

    public async Task<AppointmentAvailabilityDto> GetAvailabilityAsync(string publicCode, DateOnly date, Guid serviceId, CancellationToken cancellationToken = default)
    {
        var company = await GetCompanyAsync(publicCode, cancellationToken);
        return await _appointments.GetAvailabilityAsync(await CreateSessionAsync(company, cancellationToken), date, serviceId, null, cancellationToken);
    }

    public async Task<PublicAppointmentCreatedDto> CreateRequestAsync(string publicCode, PublicAppointmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var company = await GetCompanyAsync(publicCode, cancellationToken);
        var phone = DeliveryCustomerProfile.NormalizePhone(request.PhoneNumber);
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var companyRequests = await _context.Appointments.AsNoTracking().CountAsync(item =>
            item.CompanyId == company.Id && item.TenantId == company.TenantId &&
            item.PublicAccessTokenHash != null && item.CreatedAtUtc >= cutoff, cancellationToken);
        if (companyRequests >= 50) throw new PublicAbuseLimitException();

        var phoneRequests = await _context.Appointments.AsNoTracking().CountAsync(item =>
            item.CompanyId == company.Id && item.TenantId == company.TenantId &&
            item.PublicAccessTokenHash != null && item.CreatedAtUtc >= cutoff &&
            item.Pet.CustomerProfile.Phone == phone, cancellationToken);
        if (phoneRequests >= 5) throw new PublicAbuseLimitException();

        var customer = await _context.DeliveryCustomerProfiles.FirstOrDefaultAsync(item => item.CompanyId == company.Id && item.Phone == phone, cancellationToken);
        if (customer is null)
        {
            customer = new DeliveryCustomerProfile(company.TenantId, company.Id, phone, request.CustomerName, null, null, null, null, null, DateTime.UtcNow);
            await _context.DeliveryCustomerProfiles.AddAsync(customer, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            customer.UpdateOwnerData(request.CustomerName, customer.DeliveryAddress, customer.DeliveryNumber, customer.DeliveryNeighborhood, customer.DeliveryComplement, customer.DeliveryPostalCode);
        }

        var pet = await _context.Pets.FirstOrDefaultAsync(item => item.CompanyId == company.Id && item.CustomerProfileId == customer.Id && item.IsActive && item.Name == request.PetName.Trim(), cancellationToken);
        if (pet is null)
        {
            pet = new Pet(company.TenantId, company.Id, customer.Id, request.PetName, request.PetSpecies, request.PetSize, request.PetBreed);
            await _context.Pets.AddAsync(pet, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var session = await CreateSessionAsync(company, cancellationToken);
        var created = await _appointments.CreateAsync(session, new CreateAppointmentRequestDto
        { PetId = pet.Id, MenuItemId = request.ServiceId, StartsAtUtc = request.StartsAtUtc, CustomerNotes = request.Notes }, cancellationToken);
        var appointment = await _context.Appointments.FirstAsync(item => item.Id == created.Id, cancellationToken);
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var expiresAt = DateTime.UtcNow.AddDays(30);
        appointment.SetPublicAccess(Hash(rawToken), expiresAt);
        await _context.SaveChangesAsync(cancellationToken);
        return new PublicAppointmentCreatedDto { AccessToken = rawToken, AccessExpiresAtUtc = expiresAt, Status = appointment.Status, StartsAtUtc = appointment.StartsAtUtc };
    }

    public async Task<PublicAppointmentTrackingDto> GetTrackingAsync(string accessToken, CancellationToken cancellationToken = default)
        => Map(await GetByTokenAsync(accessToken, cancellationToken));

    public async Task<PublicAppointmentTrackingDto> CancelAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var appointment = await GetByTokenAsync(accessToken, cancellationToken, tracking: true);
        appointment.Cancel("Cancelado pelo cliente", DateTime.UtcNow);
        appointment.RevokePublicAccess(DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
        return Map(appointment);
    }

    private async Task<Company> GetCompanyAsync(string code, CancellationToken cancellationToken)
    {
        var normalized = string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToLowerInvariant();
        var company = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(item => item.PetShopPublicCode == normalized && item.BusinessSegment == BusinessSegment.PetShop && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Pet Shop nao encontrado.");
        await EnsurePublicBookingEnabledAsync(company.TenantId, cancellationToken);
        return company;
    }

    private async Task<Appointment> GetByTokenAsync(string token, CancellationToken cancellationToken, bool tracking = false)
    {
        var hash = Hash(token);
        var query = _context.Appointments.Include(item => item.Pet).AsQueryable(); if (!tracking) query = query.AsNoTracking();
        var appointment = await query.FirstOrDefaultAsync(item => item.PublicAccessTokenHash == hash && item.PublicAccessExpiresAtUtc > DateTime.UtcNow && item.PublicAccessRevokedAtUtc == null && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Agendamento nao encontrado ou link expirado.");
        await EnsurePublicBookingEnabledAsync(appointment.TenantId, cancellationToken);
        return appointment;
    }

    private async Task<WorkspaceSessionContext> CreateSessionAsync(Company company, CancellationToken cancellationToken)
    {
        var subscription = await EnsurePublicBookingEnabledAsync(company.TenantId, cancellationToken);
        var capabilities = BusinessCapabilities.Resolve(
            company.BusinessSegment,
            subscription.ProductType,
            subscription.IncludesMenuModule,
            subscription.IncludesTablesModule,
            subscription.IncludesKitchenModule,
            subscription.IncludesCashModule,
            subscription.IncludesDeliveryModule,
            subscription.IncludesPrintingModule,
            subscription.IncludesWaiterCallModule,
            subscription.IncludesAiAssistantModule,
            hasCoupons: false,
            hasReports: false);

        BusinessCapabilityGuard.Require(capabilities.HasPublicBooking, "PublicBooking");
        return new WorkspaceSessionContext
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            BusinessSegment = company.BusinessSegment,
            ProductType = subscription.ProductType,
            Capabilities = capabilities
        };
    }

    private async Task<Subscription> EnsurePublicBookingEnabledAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var subscription = await _context.Subscriptions
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.IsActive &&
                (item.Status == SubscriptionStatus.Active || item.Status == SubscriptionStatus.Trial))
            .OrderByDescending(item => item.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Pet Shop nao encontrado.");

        if (subscription.ProductType != SubscriptionProductType.PetShop || !subscription.HasPaidAccess(utcNow))
        {
            throw new KeyNotFoundException("Pet Shop nao encontrado.");
        }

        return subscription;
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()))).ToLowerInvariant();
    private static PublicAppointmentTrackingDto Map(Appointment item) => new()
    {
        PetName = item.Pet.Name, ServiceName = item.ServiceNameSnapshot, StartsAtUtc = item.StartsAtUtc, EndsAtUtc = item.EndsAtUtc,
        Status = item.Status, CanCancel = item.Status is AppointmentStatus.Requested or AppointmentStatus.Confirmed
    };
}
