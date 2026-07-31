using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Security;

public static class ZeroPaperSecurity
{
    public const string AuthenticationScheme = "ZeroPaperSession";
    public const string SessionCookie = "ZeroPaper.Session";
    public const string CsrfHeader = "X-ZP-CSRF";
    public const string SessionContextItem = "ZeroPaper.WorkspaceSession";
    public const string RootPolicy = "RootOnly";
    public const string OwnerPolicy = "OwnerOnly";
    public const string WorkspacePolicy = "WorkspaceUser";
}

public sealed class ZeroPaperSessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IAuthSessionService _sessions;

    public ZeroPaperSessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IAuthSessionService sessions) : base(options, logger, encoder)
    {
        _sessions = sessions;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization) &&
            Request.Cookies.TryGetValue(ZeroPaperSecurity.SessionCookie, out var cookieToken) &&
            !string.IsNullOrWhiteSpace(cookieToken))
        {
            authorization = $"Bearer {cookieToken}";
        }

        if (string.IsNullOrWhiteSpace(authorization))
        {
            return AuthenticateResult.NoResult();
        }

        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("Invalid authorization scheme.");
        }

        var session = await _sessions.GetSessionAsync(authorization, Context.RequestAborted);
        if (session is null)
        {
            return AuthenticateResult.Fail("Invalid or expired session.");
        }

        Context.Items[ZeroPaperSecurity.SessionContextItem] = session;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new Claim(ClaimTypes.Name, session.FullName),
            new Claim(ClaimTypes.Email, session.Email),
            new Claim(ClaimTypes.Role, session.Role),
            new Claim("tenant_id", session.TenantId.ToString()),
            new Claim("company_id", session.CompanyId.ToString())
        };
        var identity = new ClaimsIdentity(claims, ZeroPaperSecurity.AuthenticationScheme);
        return AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(identity), ZeroPaperSecurity.AuthenticationScheme));
    }
}

public static class HttpContextSessionExtensions
{
    public static WorkspaceSessionContext GetWorkspaceSession(this HttpContext context) =>
        context.Items.TryGetValue(ZeroPaperSecurity.SessionContextItem, out var value) && value is WorkspaceSessionContext session
            ? session
            : throw new UnauthorizedAccessException("Authenticated session context is unavailable.");
}

public sealed class PublicAbuseLimitException : Exception
{
    public PublicAbuseLimitException() : base("Public request limit exceeded.") { }
}
