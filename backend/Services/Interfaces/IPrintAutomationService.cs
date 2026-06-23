using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IPrintAutomationService
{
    Task<PrintingSettingsDto> GetPrintingSettingsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<PrintingSettingsDto> UpdatePrintingSettingsAsync(WorkspaceSessionContext session, UpdatePrintingSettingsRequestDto request, CancellationToken cancellationToken = default);
    Task<RotatePrintingAgentKeyResponseDto> RotatePrintingAgentKeyAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<PrintTestJobResponseDto> CreateTestJobAsync(WorkspaceSessionContext session, CreatePrintTestJobRequestDto request, CancellationToken cancellationToken = default);
    Task RequeueOrderPrintAsync(WorkspaceSessionContext session, Guid orderId, CancellationToken cancellationToken = default);
    Task<PrintAgentRegistrationResponseDto> RegisterAgentAsync(string agentKey, PrintAgentHeartbeatRequestDto request, CancellationToken cancellationToken = default);
    Task RegisterAgentHeartbeatAsync(string agentKey, PrintAgentHeartbeatRequestDto request, CancellationToken cancellationToken = default);
    Task<PrintAgentOrderJobDto?> ClaimNextOrderJobAsync(string agentKey, PrintAgentClaimRequestDto request, CancellationToken cancellationToken = default);
    Task CompleteOrderJobAsync(string agentKey, Guid orderId, CompletePrintJobRequestDto request, CancellationToken cancellationToken = default);
    Task FailOrderJobAsync(string agentKey, Guid orderId, FailPrintJobRequestDto request, CancellationToken cancellationToken = default);
    Task CompleteOrderBatchAsync(string agentKey, CompletePrintJobBatchRequestDto request, CancellationToken cancellationToken = default);
    Task FailOrderBatchAsync(string agentKey, FailPrintJobBatchRequestDto request, CancellationToken cancellationToken = default);
}
