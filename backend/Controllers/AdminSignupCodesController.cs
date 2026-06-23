using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/admin/signup-codes")]
public class AdminSignupCodesController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IAdminSignupCodeService _adminSignupCodeService;

    public AdminSignupCodesController(IAuthSessionService authSessionService, IAdminSignupCodeService adminSignupCodeService)
    {
        _authSessionService = authSessionService;
        _adminSignupCodeService = adminSignupCodeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SignupCodeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSignupCodesAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _adminSignupCodeService.GetSignupCodesAsync(session, cancellationToken));
    }

    [HttpPost]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(CreateSignupCodeResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSignupCodeAsync([FromBody] CreateSignupCodeRequestDto request, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        var response = await _adminSignupCodeService.CreateSignupCodeAsync(session, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpDelete("{codeId:guid}")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSignupCodeAsync(Guid codeId, CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);

        if (session is null)
        {
            return Unauthorized();
        }

        await _adminSignupCodeService.DeleteSignupCodeAsync(session, codeId, cancellationToken);
        return NoContent();
    }

    [HttpPost("cleanup")]
    [EnableRateLimiting("sensitive-write")]
    [ProducesResponseType(typeof(CleanupSignupCodesResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CleanupSignupCodesAsync(CancellationToken cancellationToken)
    {
        var session = await GetRequiredSessionAsync(cancellationToken);
        return session is null
            ? Unauthorized()
            : Ok(await _adminSignupCodeService.CleanupSignupCodesAsync(session, cancellationToken));
    }

    private async Task<WorkspaceSessionContext?> GetRequiredSessionAsync(CancellationToken cancellationToken)
    {
        return await _authSessionService.GetSessionAsync(Request.Headers.Authorization.ToString(), cancellationToken);
    }
}
