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

    public AuthController(IAuthSessionService authSessionService)
    {
        _authSessionService = authSessionService;
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
}

