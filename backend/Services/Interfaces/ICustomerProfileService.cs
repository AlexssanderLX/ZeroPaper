using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface ICustomerProfileService
{
    Task<CustomerProfileListDto> SearchAsync(WorkspaceSessionContext session, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<CustomerProfileDto> GetByIdAsync(WorkspaceSessionContext session, Guid customerId, CancellationToken cancellationToken = default);
    Task<CustomerProfileDto> CreateAsync(WorkspaceSessionContext session, CreateCustomerProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<CustomerProfileDto> UpdateAsync(WorkspaceSessionContext session, Guid customerId, UpdateCustomerProfileRequestDto request, CancellationToken cancellationToken = default);
}
