using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IPetService
{
    Task<IReadOnlyList<PetDto>> GetAsync(WorkspaceSessionContext session, Guid? customerProfileId = null, CancellationToken cancellationToken = default);
    Task<PetDto> GetByIdAsync(WorkspaceSessionContext session, Guid petId, CancellationToken cancellationToken = default);
    Task<PetDto> CreateAsync(WorkspaceSessionContext session, CreatePetRequestDto request, CancellationToken cancellationToken = default);
    Task<PetDto> UpdateAsync(WorkspaceSessionContext session, Guid petId, UpdatePetRequestDto request, CancellationToken cancellationToken = default);
    Task<PetDto> UpdateStatusAsync(WorkspaceSessionContext session, Guid petId, bool isActive, CancellationToken cancellationToken = default);
}
