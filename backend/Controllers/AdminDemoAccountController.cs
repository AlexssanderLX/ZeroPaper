using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Security;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
[Authorize(Policy = ZeroPaperSecurity.RootPolicy)]
[Route("api/admin/demo-account")]
public class AdminDemoAccountController : ControllerBase
{
    private readonly IAdminDemoAccountService _demoAccounts;

    public AdminDemoAccountController(IAdminDemoAccountService demoAccounts) => _demoAccounts = demoAccounts;

    [HttpGet]
    [ProducesResponseType(typeof(AdminDemoAccountDto), StatusCodes.Status200OK)]
    public Task<AdminDemoAccountDto> GetAsync(CancellationToken cancellationToken) =>
        _demoAccounts.GetAsync(HttpContext.GetWorkspaceSession(), cancellationToken);

    [HttpPost]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(AdminDemoAccountDto), StatusCodes.Status200OK)]
    public Task<AdminDemoAccountDto> EnsureAsync(CancellationToken cancellationToken) =>
        _demoAccounts.EnsureAccountAsync(HttpContext.GetWorkspaceSession(), cancellationToken);

    [HttpPost("link")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(AdminDemoLinkCreatedDto), StatusCodes.Status200OK)]
    public Task<AdminDemoLinkCreatedDto> RotateLinkAsync(CancellationToken cancellationToken) =>
        _demoAccounts.RotateLinkAsync(HttpContext.GetWorkspaceSession(), cancellationToken);

    [HttpDelete("link")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(AdminDemoAccountDto), StatusCodes.Status200OK)]
    public Task<AdminDemoAccountDto> RevokeLinkAsync(CancellationToken cancellationToken) =>
        _demoAccounts.RevokeLinkAsync(HttpContext.GetWorkspaceSession(), cancellationToken);

    [HttpDelete("sessions")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(AdminDemoAccountDto), StatusCodes.Status200OK)]
    public Task<AdminDemoAccountDto> RevokeSessionsAsync(CancellationToken cancellationToken) =>
        _demoAccounts.RevokeSessionsAsync(HttpContext.GetWorkspaceSession(), cancellationToken);
}
