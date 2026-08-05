using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Security;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Controllers;

[ApiController]
[Authorize(Policy = ZeroPaperSecurity.RootPolicy)]
[Route("api/admin/platform-billing")]
public sealed class AdminPlatformBillingController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IPlatformBillingService _billingService;

    public AdminPlatformBillingController(IAuthSessionService authSessionService, IPlatformBillingService billingService)
    {
        _authSessionService = authSessionService;
        _billingService = billingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        return session is null ? Unauthorized() : Ok(await _billingService.GetStatusAsync(session, cancellationToken));
    }

    [HttpPut("mercadopago")]
    [EnableRateLimiting("sensitive-write")]
    public async Task<IActionResult> ConfigureAsync([FromBody] ConfigureAdminPlatformBillingRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        return session is null ? Unauthorized() : Ok(await _billingService.ConfigureAsync(session, request, cancellationToken));
    }

    [HttpDelete("mercadopago")]
    [EnableRateLimiting("sensitive-write")]
    public async Task<IActionResult> DisconnectAsync([FromBody] AdminSensitiveActionRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        if (session is null) return Unauthorized();
        await _billingService.DisconnectAsync(session, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("companies/{companyId:guid}/checkout")]
    [EnableRateLimiting("sensitive-write")]
    public async Task<IActionResult> CreateCheckoutAsync(Guid companyId, [FromBody] AdminSensitiveActionRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        return session is null ? Unauthorized() : Ok(await _billingService.CreateSubscriptionCheckoutAsync(session, companyId, request, cancellationToken));
    }

    [HttpPost("companies/{companyId:guid}/sync")]
    [EnableRateLimiting("sensitive-write")]
    public async Task<IActionResult> SyncAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(cancellationToken);
        return session is null ? Unauthorized() : Ok(await _billingService.SyncSubscriptionAsync(session, companyId, cancellationToken));
    }

    private Task<WorkspaceSessionContext?> GetSessionAsync(CancellationToken cancellationToken) =>
        _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
}
