using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface ISalesAgentService
{
    Task<IReadOnlyList<SalesAgentDto>> GetByCompanyAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<SalesAgentDto> CreateAsync(WorkspaceSessionContext session, CreateSalesAgentRequestDto request, CancellationToken cancellationToken = default);
    Task<SalesAgentDto> UpdateAsync(WorkspaceSessionContext session, Guid agentId, UpdateSalesAgentRequestDto request, CancellationToken cancellationToken = default);
    Task<SalesAgentDto> UpdateStatusAsync(WorkspaceSessionContext session, Guid agentId, bool isActive, CancellationToken cancellationToken = default);
    Task<PublicSellerLinkDto> GetPublicSellerLinkAsync(string code, CancellationToken cancellationToken = default);
}
