using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public sealed class PetService : IPetService
{
    private readonly ZeroPaperDbContext _context;
    private readonly string _uploadsRootPath;

    public PetService(ZeroPaperDbContext context, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _context = context;
        var configured = configuration["Storage:UploadsPath"] ?? Environment.GetEnvironmentVariable("ZEROPAPER_UPLOADS_PATH");
        _uploadsRootPath = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? (environment.IsDevelopment() ? Path.Combine(environment.ContentRootPath, "wwwroot", "uploads") : Path.Combine("/var/lib/zeropaper", "uploads"))
            : configured);
    }

    internal PetService(ZeroPaperDbContext context, string uploadsRootPath)
    {
        _context = context;
        _uploadsRootPath = Path.GetFullPath(uploadsRootPath);
    }

    public async Task<PetListDto> GetAsync(
        WorkspaceSessionContext session,
        string? search,
        Guid? customerProfileId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);

        var query = _context.Pets
            .AsNoTracking()
            .Include(item => item.CustomerProfile)
            .Where(item => item.TenantId == session.TenantId && item.CompanyId == session.CompanyId);

        if (customerProfileId.HasValue)
        {
            query = query.Where(item => item.CustomerProfileId == customerProfileId.Value);
        }

        if (isActive.HasValue) query = query.Where(item => item.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            query = query.Where(item => item.Name.Contains(normalized) || (item.Breed != null && item.Breed.Contains(normalized)));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);

        var pets = await query
            .OrderBy(item => item.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PetListDto { Items = pets.Select(MapToDto).ToList(), Page = page, PageSize = pageSize, Total = total };
    }

    public async Task<PetDto> GetByIdAsync(
        WorkspaceSessionContext session,
        Guid petId,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        return MapToDto(await GetEntityAsync(session, petId, tracking: false, cancellationToken));
    }

    public async Task<PetDto> CreateAsync(
        WorkspaceSessionContext session,
        CreatePetRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        ArgumentNullException.ThrowIfNull(request);

        var customer = await _context.DeliveryCustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == request.CustomerProfileId &&
                item.TenantId == session.TenantId &&
                item.CompanyId == session.CompanyId &&
                item.IsActive,
                cancellationToken)
            ?? throw new KeyNotFoundException("Cliente nao encontrado.");

        var pet = new Pet(
            session.TenantId,
            session.CompanyId,
            customer.Id,
            request.Name,
            request.Species,
            request.Size,
            request.Breed,
            request.WeightKg,
            request.BirthDate);

        pet.UpdateNotes(request.BehaviorNotes, request.AllergyNotes, request.Restrictions);
        await _context.Pets.AddAsync(pet, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(session, pet.Id, cancellationToken);
    }

    public async Task<PetDto> UpdateAsync(
        WorkspaceSessionContext session,
        Guid petId,
        UpdatePetRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        ArgumentNullException.ThrowIfNull(request);

        var pet = await GetEntityAsync(session, petId, tracking: true, cancellationToken);

        pet.UpdateBasicInformation(request.Name, request.Species, request.Size, request.Breed, request.WeightKg, request.BirthDate);
        pet.UpdateNotes(request.BehaviorNotes, request.AllergyNotes, request.Restrictions);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(session, pet.Id, cancellationToken);
    }

    public async Task<PetPhotoDto> UploadPhotoAsync(WorkspaceSessionContext session, Guid petId, IFormFile file, CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        if (file.Length is <= 0 or > 5 * 1024 * 1024) throw new ArgumentException("A foto deve ter no maximo 5 MB.", nameof(file));
        var pet = await GetEntityAsync(session, petId, tracking: true, cancellationToken);
        var extension = await SafeUploadValidator.GetImageExtensionAsync(file, cancellationToken);
        var relativeDirectory = Path.Combine("pets", session.CompanyId.ToString("N"));
        var directory = Path.Combine(_uploadsRootPath, relativeDirectory);
        Directory.CreateDirectory(directory);
        var fileName = $"{pet.Id:N}-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(directory, fileName);
        await using (var stream = File.Create(fullPath)) await file.CopyToAsync(stream, cancellationToken);

        DeleteOwnedPhotoIfPresent(pet.PhotoUrl, directory);
        var url = $"/uploads/pets/{session.CompanyId:N}/{fileName}";
        pet.SetPhoto(url);
        await _context.SaveChangesAsync(cancellationToken);
        return new PetPhotoDto { PetId = pet.Id, PhotoUrl = pet.PhotoUrl };
    }

    public async Task<PetPhotoDto> RemovePhotoAsync(WorkspaceSessionContext session, Guid petId, CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        var pet = await GetEntityAsync(session, petId, tracking: true, cancellationToken);
        var directory = Path.Combine(_uploadsRootPath, "pets", session.CompanyId.ToString("N"));
        DeleteOwnedPhotoIfPresent(pet.PhotoUrl, directory);
        pet.SetPhoto(null);
        await _context.SaveChangesAsync(cancellationToken);
        return new PetPhotoDto { PetId = pet.Id };
    }

    private static void DeleteOwnedPhotoIfPresent(string? photoUrl, string allowedDirectory)
    {
        if (string.IsNullOrWhiteSpace(photoUrl)) return;
        var fileName = Path.GetFileName(photoUrl);
        if (string.IsNullOrWhiteSpace(fileName)) return;
        var allowedRoot = Path.GetFullPath(allowedDirectory) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(allowedDirectory, fileName));
        if (candidate.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(candidate)) File.Delete(candidate);
    }

    public async Task<PetDto> UpdateStatusAsync(
        WorkspaceSessionContext session,
        Guid petId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        var pet = await GetEntityAsync(session, petId, tracking: true, cancellationToken);

        if (!isActive)
        {
            var hasFutureAppointment = await _context.Appointments.AnyAsync(item =>
                item.PetId == pet.Id && item.CompanyId == session.CompanyId && item.TenantId == session.TenantId &&
                item.StartsAtUtc >= DateTime.UtcNow && item.Status != AppointmentStatus.Cancelled &&
                item.Status != AppointmentStatus.Completed && item.Status != AppointmentStatus.NoShow,
                cancellationToken);
            if (hasFutureAppointment) throw new InvalidOperationException("O animal possui agendamentos futuros ativos.");
        }

        if (isActive) pet.Activate(); else pet.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(session, pet.Id, cancellationToken);
    }

    private async Task<Pet> GetEntityAsync(
        WorkspaceSessionContext session,
        Guid petId,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var query = _context.Pets.Include(item => item.CustomerProfile).AsQueryable();
        if (!tracking) query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(item =>
            item.Id == petId &&
            item.TenantId == session.TenantId &&
            item.CompanyId == session.CompanyId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Animal nao encontrado.");
    }

    private static void EnsurePetShop(WorkspaceSessionContext session)
    {
        BusinessCapabilityGuard.Require(session.Capabilities.HasPets, "Pets");
    }

    private static PetDto MapToDto(Pet pet) => new()
    {
        Id = pet.Id,
        CustomerProfileId = pet.CustomerProfileId,
        CustomerName = pet.CustomerProfile.CustomerName ?? pet.CustomerProfile.Phone,
        Name = pet.Name,
        Species = pet.Species,
        Size = pet.Size,
        Breed = pet.Breed,
        WeightKg = pet.WeightKg,
        BirthDate = pet.BirthDate,
        BehaviorNotes = pet.BehaviorNotes,
        AllergyNotes = pet.AllergyNotes,
        Restrictions = pet.Restrictions,
        PhotoUrl = pet.PhotoUrl,
        IsActive = pet.IsActive
    };
}
