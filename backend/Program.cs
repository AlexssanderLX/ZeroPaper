using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using QuestPDF.Infrastructure;
using System.Net;
using System.Net.Mime;
using System.Threading.RateLimiting;
using ZeroPaper.Data;
using ZeroPaper.Repositories;
using ZeroPaper.Repositories.Interfaces;
using ZeroPaper.Services;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;
using ZeroPaper.Services.Reports;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
}
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection was not configured.");

var allowedOrigins = builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];
var enableHttpsRedirection = builder.Configuration.GetValue("Security:EnableHttpsRedirection", false);
var httpsPort = builder.Configuration.GetValue<int?>("Security:HttpsPort");
var dataProtectionPath = builder.Configuration["Security:DataProtectionPath"];
var uploadsPath = builder.Configuration["Storage:UploadsPath"];

if (string.IsNullOrWhiteSpace(dataProtectionPath))
{
    dataProtectionPath = builder.Environment.IsDevelopment()
        ? Path.Combine(builder.Environment.ContentRootPath, ".dataprotection")
        : Path.Combine("/var/lib/zeropaper", "dataprotection");
}

if (string.IsNullOrWhiteSpace(uploadsPath))
{
    uploadsPath = builder.Environment.IsDevelopment()
        ? Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads")
        : Path.Combine("/var/lib/zeropaper", "uploads");
}

Directory.CreateDirectory(dataProtectionPath);
Directory.CreateDirectory(uploadsPath);

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("ZeroPaper");
if (enableHttpsRedirection && httpsPort.HasValue)
{
    builder.Services.AddHttpsRedirection(options => options.HttpsPort = httpsPort.Value);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => IsAllowedFrontendOrigin(origin, allowedOrigins, builder.Environment.IsDevelopment()))
            .WithHeaders("Content-Type", "Authorization")
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS");
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("public-write", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 10;
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("integration-write", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 120;
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("webhook-ingress", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 30000;
        limiter.QueueLimit = 1000;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("sensitive-write", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 12;
        limiter.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("upload-write", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 20;
        limiter.QueueLimit = 0;
    });
});

builder.Services.AddDbContext<ZeroPaperDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mysqlOptions => mysqlOptions
            .MigrationsHistoryTable("__efmigrationshistory")
            .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
);

builder.Services.Configure<OpenAiApiOptions>(builder.Configuration.GetSection(OpenAiApiOptions.SectionName));
builder.Services.Configure<PublicAppOptions>(builder.Configuration.GetSection(PublicAppOptions.SectionName));
builder.Services.Configure<EvolutionApiOptions>(builder.Configuration.GetSection(EvolutionApiOptions.SectionName));
builder.Services.Configure<DeliveryDistanceOptions>(builder.Configuration.GetSection(DeliveryDistanceOptions.SectionName));
builder.Services.Configure<MercadoPagoOptions>(builder.Configuration.GetSection(MercadoPagoOptions.SectionName));

builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IAppUserRepository, AppUserRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IQrCodeAccessRepository, QrCodeAccessRepository>();
builder.Services.AddScoped<ISalesAgentRepository, SalesAgentRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ICashOrderTableService, CashOrderTableService>();
builder.Services.AddScoped<IAuthSessionService, AuthSessionService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IAdminSignupCodeService, AdminSignupCodeService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminOwnerService, AdminOwnerService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAccessRequestNotificationService, SmtpAccessRequestNotificationService>();
builder.Services.AddScoped<IRestaurantOnboardingService, RestaurantOnboardingService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<ISalesAgentService, SalesAgentService>();
builder.Services.AddScoped<ISalesReportService, SalesReportService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<ICashClosingService, CashClosingService>();
builder.Services.AddScoped<IDeliveryFreightService, DeliveryFreightService>();
builder.Services.AddScoped<IDeliveryCustomerLinkService, DeliveryCustomerLinkService>();
builder.Services.AddHttpClient<ApproximatePostalCodeDeliveryDistanceProvider>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DeliveryDistanceOptions>>().Value;
    var baseUrl = string.IsNullOrWhiteSpace(options.Approximate.BaseUrl)
        ? "https://nominatim.openstreetmap.org/"
        : options.Approximate.BaseUrl.Trim();

    if (!baseUrl.EndsWith('/'))
    {
        baseUrl += "/";
    }

    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddScoped<IDeliveryDistanceProvider>(serviceProvider => serviceProvider.GetRequiredService<ApproximatePostalCodeDeliveryDistanceProvider>());
builder.Services.AddScoped<IDeliveryDistanceProvider, MockDeliveryDistanceProvider>();
builder.Services.AddScoped<IPrintAutomationService, PrintAutomationService>();
builder.Services.AddHttpClient<GoogleRoutesDeliveryDistanceProvider>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DeliveryDistanceOptions>>().Value;
    var baseUrl = string.IsNullOrWhiteSpace(options.GoogleMaps.BaseUrl)
        ? "https://routes.googleapis.com/"
        : options.GoogleMaps.BaseUrl.Trim();

    if (!baseUrl.EndsWith('/'))
    {
        baseUrl += "/";
    }

    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(12);
});
builder.Services.AddScoped<IDeliveryDistanceProvider>(serviceProvider => serviceProvider.GetRequiredService<GoogleRoutesDeliveryDistanceProvider>());
builder.Services.AddHttpClient<IAiAssistantService, AiAssistantService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAiApiOptions>>().Value;
    var configuredBaseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
        ? Environment.GetEnvironmentVariable("OPENAI_BASE_URL")
        : options.BaseUrl;
    var baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
        ? OpenAiApiOptions.DefaultBaseUrl
        : configuredBaseUrl.Trim();
    if (!baseUrl.EndsWith('/'))
    {
        baseUrl += "/";
    }

    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(35);
});
builder.Services.AddHttpClient<IWhatsAppIntegrationService, WhatsAppIntegrationService>(client =>
{
    client.BaseAddress = new Uri("https://api.z-api.io/", UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddHttpClient<IMercadoPagoService, MercadoPagoService>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MercadoPagoOptions>>().Value;
    var baseUrl = string.IsNullOrWhiteSpace(options.ApiBaseUrl)
        ? MercadoPagoOptions.DefaultApiBaseUrl
        : options.ApiBaseUrl.Trim();

    if (!baseUrl.EndsWith('/'))
    {
        baseUrl += "/";
    }

    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddSingleton<PlatformRootSeeder>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var rootSeeder = scope.ServiceProvider.GetRequiredService<PlatformRootSeeder>();
    await rootSeeder.EnsureRootAccountAsync();
    var cashOrderTableService = scope.ServiceProvider.GetRequiredService<ICashOrderTableService>();
    await cashOrderTableService.EnsureForActiveOwnersAsync();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var statusCode = feature?.Error switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            InvalidOperationException => StatusCodes.Status409Conflict,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = MediaTypeNames.Application.Json;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode == StatusCodes.Status500InternalServerError
                ? "Unexpected error"
                : "Request could not be processed",
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "An internal error occurred."
                : "The request was rejected."
        };

        await context.Response.WriteAsJsonAsync(problem);
    });
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'; form-action 'self'";

    await next();
});

app.UseRateLimiter();
app.UseCors("frontend");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});
app.UseStaticFiles();

if (enableHttpsRedirection && httpsPort.HasValue)
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

static bool IsAllowedFrontendOrigin(string? origin, string[] allowedOrigins, bool isDevelopment)
{
    if (string.IsNullOrWhiteSpace(origin))
    {
        return false;
    }

    if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
    {
        return true;
    }

    if (!isDevelopment || !Uri.TryCreate(origin, UriKind.Absolute, out var uri))
    {
        return false;
    }

    if (uri.Scheme is not ("http" or "https"))
    {
        return false;
    }

    if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host.Equals("127.0.0.1"))
    {
        return true;
    }

    if (!IPAddress.TryParse(uri.Host, out var ipAddress))
    {
        return false;
    }

    if (ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
    {
        return false;
    }

    var bytes = ipAddress.GetAddressBytes();

    return bytes[0] == 10
        || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
        || (bytes[0] == 192 && bytes[1] == 168);
}
