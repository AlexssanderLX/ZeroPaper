using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IPetService
{
    Task<PetListDto> GetAsync(WorkspaceSessionContext session, string? search, Guid? customerProfileId, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PetDto> GetByIdAsync(WorkspaceSessionContext session, Guid petId, CancellationToken cancellationToken = default);
    Task<PetDto> CreateAsync(WorkspaceSessionContext session, CreatePetRequestDto request, CancellationToken cancellationToken = default);
    Task<PetDto> UpdateAsync(WorkspaceSessionContext session, Guid petId, UpdatePetRequestDto request, CancellationToken cancellationToken = default);
    Task<PetDto> UpdateStatusAsync(WorkspaceSessionContext session, Guid petId, bool isActive, CancellationToken cancellationToken = default);
    Task<PetPhotoDto> UploadPhotoAsync(WorkspaceSessionContext session, Guid petId, IFormFile file, CancellationToken cancellationToken = default);
    Task<PetPhotoDto> RemovePhotoAsync(WorkspaceSessionContext session, Guid petId, CancellationToken cancellationToken = default);
}
