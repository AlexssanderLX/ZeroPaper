using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Interfaces;

public interface ICashClosingService
{
    Task<CashClosingReportDto> GetCashClosingAsync(
        WorkspaceSessionContext session,
        DateOnly referenceDate,
        CancellationToken cancellationToken = default);
}
