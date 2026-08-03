using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using ZeroPaper.DTOs.Auth;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Security;

namespace ZeroPaper.Controllers;

[ApiController]
[RequestSizeLimit(16 * 1024)]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthSessionService _authSessionService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly LoginAttemptProtector _loginAttempts;

    public AuthController(IAuthSessionService authSessionService, IPasswordResetService passwordResetService, LoginAttemptProtector loginAttempts)
    {
        _authSessionService = authSessionService;
        _passwordResetService = passwordResetService;
        _loginAttempts = loginAttempts;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            await _loginAttempts.DelayIfNeededAsync(HttpContext, request.Email, cancellationToken);
            var response = await _authSessionService.LoginAsync(request, cancellationToken);
            if (response is null)
            {
                _loginAttempts.RecordFailure(HttpContext, request.Email);
                return Unauthorized();
            }
            _loginAttempts.Reset(HttpContext, request.Email);
            SetSessionCookie(response.Token, response.ExpiresAtUtc);
            response.Token = string.Empty;
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Dados invalidos",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Acesso negado",
                Detail = exception.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }
    }

    [HttpPost("shortcut-login")]
    [AllowAnonymous]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ShortcutLoginAsync([FromBody] ShortcutLoginRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authSessionService.LoginWithShortcutAsync(request, cancellationToken);
            if (response is null) return Unauthorized();
            SetSessionCookie(response.Token, response.ExpiresAtUtc);
            response.Token = string.Empty;
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Dados invalidos",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Atalho indisponivel",
                Detail = exception.Message,
                Status = StatusCodes.Status403Forbidden
            });
        }
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        await _authSessionService.LogoutAsync(GetSessionAuthorization(), cancellationToken);
        Response.Cookies.Delete(ZeroPaperSecurity.SessionCookie, new CookieOptions { Path = "/" });
        return NoContent();
    }

    [HttpPost("password/request-reset")]
    [AllowAnonymous]
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
    [AllowAnonymous]
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

    [HttpPost("confirm-password")]
    [EnableRateLimiting("public-write")]
    [ProducesResponseType(typeof(ConfirmPasswordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ConfirmPasswordAsync(
        [FromBody] ConfirmPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = HttpContext.GetWorkspaceSession();

        return Ok(new ConfirmPasswordResponseDto
        {
            Confirmed = await _authSessionService.ConfirmPasswordAsync(
                GetSessionAuthorization(),
                request.Password,
                cancellationToken)
        });
    }

    private void SetSessionCookie(string token, DateTime expiresAtUtc)
    {
        Response.Cookies.Append(ZeroPaperSecurity.SessionCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = new DateTimeOffset(expiresAtUtc),
            IsEssential = true
        });
    }

    private string GetSessionAuthorization()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(header)) return header;
        return Request.Cookies.TryGetValue(ZeroPaperSecurity.SessionCookie, out var token)
            ? $"Bearer {token}"
            : string.Empty;
    }
}
