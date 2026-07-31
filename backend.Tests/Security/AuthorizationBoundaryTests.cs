using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ZeroPaper.Data;
using Xunit;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http.Metadata;
using System.Text.RegularExpressions;

namespace ZeroPaper.Tests.Security;

public sealed class AuthorizationBoundaryTests : IClassFixture<SecurityApplicationFactory>
{
    private readonly SecurityApplicationFactory _factory;
    public AuthorizationBoundaryTests(SecurityApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/api/admin/dashboard")]
    [InlineData("/api/workspace/overview")]
    [InlineData("/api/workspace/pets")]
    [InlineData("/api/workspace/appointments?fromUtc=2026-01-01T00:00:00Z&toUtc=2026-01-02T00:00:00Z")]
    public async Task ProtectedEndpoints_Return401_WithoutSession(string path)
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Employee", "/api/admin/dashboard")]
    [InlineData("Owner", "/api/admin/dashboard")]
    [InlineData("Root", "/api/workspace/overview")]
    public async Task RolePolicies_Return403_ForWrongRole(string role, string path)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ExplicitPublicEndpoint_RemainsAnonymous()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/public/segments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedCookieMutation_RequiresCsrfHeader()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/workspace/pets/{Guid.NewGuid()}");
        request.Headers.Add("X-Test-Role", "Owner");
        request.Headers.Add("Cookie", "ZeroPaper.Session=test-session");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("CSRF", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EveryAdminAndWorkspaceEndpoint_Enforces401AndRole403()
    {
        using var anonymous = _factory.CreateClient();
        using var employee = _factory.CreateClient();
        using var root = _factory.CreateClient();
        employee.DefaultRequestHeaders.Add("X-Test-Role", "Employee");
        root.DefaultRequestHeaders.Add("X-Test-Role", "Root");

        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint =>
            {
                var route = endpoint.RoutePattern.RawText ?? string.Empty;
                var protectedArea = route.StartsWith("api/admin/", StringComparison.OrdinalIgnoreCase) ||
                    route.StartsWith("api/workspace", StringComparison.OrdinalIgnoreCase);
                return protectedArea &&
                    endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() is null;
            })
            .ToList();

        Assert.NotEmpty(endpoints);
        foreach (var endpoint in endpoints)
        {
            var rawRoute = endpoint.RoutePattern.RawText!;
            var route = "/" + Regex.Replace(rawRoute, "\\{[^}]+\\}", Guid.Empty.ToString());
            var method = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.FirstOrDefault() ?? "GET";

            using var anonymousResponse = await anonymous.SendAsync(new HttpRequestMessage(new HttpMethod(method), route));
            Assert.True(anonymousResponse.StatusCode == HttpStatusCode.Unauthorized,
                $"{method} {rawRoute} returned {(int)anonymousResponse.StatusCode} without authentication.");

            var roleClient = rawRoute.StartsWith("api/admin/", StringComparison.OrdinalIgnoreCase) ? employee : root;
            using var forbiddenResponse = await roleClient.SendAsync(new HttpRequestMessage(new HttpMethod(method), route));
            Assert.True(forbiddenResponse.StatusCode == HttpStatusCode.Forbidden,
                $"{method} {rawRoute} returned {(int)forbiddenResponse.StatusCode} for an incompatible role.");
        }
    }
}

public sealed class SecurityApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=127.0.0.1;Database=security_tests;User=test;Password=test"
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ZeroPaperDbContext>>();
            services.RemoveAll<ZeroPaperDbContext>();
            services.AddDbContext<ZeroPaperDbContext>(options => options.UseInMemoryDatabase("security-tests"));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestRoleAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestRoleAuthenticationHandler.SchemeName;
                options.DefaultForbidScheme = TestRoleAuthenticationHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestRoleAuthenticationHandler>(TestRoleAuthenticationHandler.SchemeName, _ => { });
        });
    }
}

public sealed class TestRoleAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestRole";
    public TestRoleAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Role", out var role) || string.IsNullOrWhiteSpace(role))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "Security Test"),
            new Claim(ClaimTypes.Role, role.ToString())
        }, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
