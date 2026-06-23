using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface IAiAssistantService
{
    Task<AiAssistantSettingsDto> GetSettingsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<AiAssistantQuickStatusDto> GetQuickStatusAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<AiAssistantQuickStatusDto> UpdateQuickStatusAsync(WorkspaceSessionContext session, bool isEnabled, CancellationToken cancellationToken = default);
    Task<AiAssistantSettingsDto> UpdateSettingsAsync(WorkspaceSessionContext session, UpdateAiAssistantSettingsRequestDto request, CancellationToken cancellationToken = default);
    Task<AiAssistantSettingsDto> GenerateTemplateAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default);
    Task<AiAssistantTestResponseDto> TestAssistantAsync(WorkspaceSessionContext session, AiAssistantTestRequestDto request, CancellationToken cancellationToken = default);
    Task<AiAssistantGeneratedReplyDto> GenerateReplyAsync(
        Guid companyId,
        string source,
        string message,
        IReadOnlyList<AiAssistantConversationTurnDto>? history = null,
        string? customerContext = null,
        CancellationToken cancellationToken = default);
}
