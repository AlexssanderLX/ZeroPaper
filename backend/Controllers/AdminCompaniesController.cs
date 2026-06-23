using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/admin/companies")]
public class AdminCompaniesController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IAdminDashboardService _adminDashboardService;

    public AdminCompaniesController(IAuthSessionService authSessionService, IAdminDashboardService adminDashboardService)
    {
        _authSessionService = authSessionService;
        _adminDashboardService = adminDashboardService;
    }

    [HttpPost("{companyId:guid}/master-password/reveal")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(AdminCompanyMasterPasswordRevealDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevealMasterPasswordAsync(
        Guid companyId,
        [FromBody] AdminSensitiveActionRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _adminDashboardService.RevealCompanyMasterPasswordAsync(session, companyId, request, cancellationToken));
    }

    [HttpPost("{companyId:guid}/master-password/rotate")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(AdminCompanyMasterPasswordRevealDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RotateMasterPasswordAsync(
        Guid companyId,
        [FromBody] AdminSensitiveActionRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _adminDashboardService.RotateCompanyMasterPasswordAsync(session, companyId, request, cancellationToken));
    }

    [HttpPatch("{companyId:guid}/plan")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(AdminCompanyPlanUpdateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePlanAsync(
        Guid companyId,
        [FromBody] UpdateAdminCompanyPlanRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _adminDashboardService.UpdateCompanyPlanAsync(session, companyId, request, cancellationToken));
    }

    [HttpDelete("{companyId:guid}")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCompanyAsync(
        Guid companyId,
        [FromBody] DeleteAdminCompanyRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        await _adminDashboardService.DeleteCompanyAsync(session, companyId, request, cancellationToken);
        return NoContent();
    }

    private async Task<WorkspaceSessionContext?> GetRequiredSessionAsync(CancellationToken cancellationToken)
    {
        return await _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
    }
}
