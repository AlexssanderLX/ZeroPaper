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

    public PetService(ZeroPaperDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PetDto>> GetAsync(
        WorkspaceSessionContext session,
        Guid? customerProfileId = null,
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

        var pets = await query
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);

        return pets.Select(MapToDto).ToList();
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

    public async Task<PetDto> UpdateStatusAsync(
        WorkspaceSessionContext session,
        Guid petId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        EnsurePetShop(session);
        var pet = await GetEntityAsync(session, petId, tracking: true, cancellationToken);

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
        if (session.BusinessSegment != BusinessSegment.PetShop)
        {
            throw new UnauthorizedAccessException("O modulo de animais nao esta disponivel para esta empresa.");
        }
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
