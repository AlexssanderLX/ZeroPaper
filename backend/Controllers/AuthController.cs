using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Auth;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IPasswordResetService _passwordResetService;

    public AuthController(IAuthSessionService authSessionService, IPasswordResetService passwordResetService)
    {
        _authSessionService = authSessionService;
        _passwordResetService = passwordResetService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authSessionService.LoginAsync(request, cancellationToken);
        return response is null ? Unauthorized() : Ok(response);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        await _authSessionService.LogoutAsync(Request.Headers.Authorization.ToString(), cancellationToken);
        return NoContent();
    }

    [HttpPost("password/request-reset")]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(typeof(PasswordResetRequestResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestPasswordResetAsync(
        [FromBody] PasswordResetRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _passwordResetService.RequestAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("password/reset")]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPasswordAsync(
        [FromBody] ResetPasswordDto request,
        CancellationToken cancellationToken)
    {
        var succeeded = await _passwordResetService.ResetAsync(request, cancellationToken);
        return succeeded ? NoContent() : BadRequest();
    }
}
