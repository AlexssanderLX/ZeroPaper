using Microsoft.AspNetCore.Mvc;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IAdminDashboardService _adminDashboardService;

    public AdminDashboardController(IAuthSessionService authSessionService, IAdminDashboardService adminDashboardService)
    {
        _authSessionService = authSessionService;
        _adminDashboardService = adminDashboardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _adminDashboardService.GetDashboardAsync(session, cancellationToken));
    }

    private async Task<WorkspaceSessionContext?> GetRequiredSessionAsync(CancellationToken cancellationToken)
    {
        return await _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
    }
}
