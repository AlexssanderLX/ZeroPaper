using ZeroPaper.DTOs.Workspace.Reports;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services.Reports;

public interface ISalesReportService
{
    Task<DailySalesReportDto> GetDailySalesReportAsync(
        WorkspaceSessionContext session,
        DateOnly referenceDate,
        CancellationToken cancellationToken = default);
}
