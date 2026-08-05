using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Admin;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public sealed class PlatformBillingService : IPlatformBillingService
{
    private const string Provider = "MercadoPago";
    private readonly ZeroPaperDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDataProtector _protector;
    private readonly HttpClient _httpClient;
    private readonly PublicAppOptions _publicAppOptions;

    public PlatformBillingService(
        ZeroPaperDbContext context,
        IPasswordHasher passwordHasher,
        IDataProtectionProvider dataProtectionProvider,
        HttpClient httpClient,
        IOptions<PublicAppOptions> publicAppOptions)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _protector = dataProtectionProvider.CreateProtector("ZeroPaper.PlatformBilling.MercadoPago.AccessToken.v1");
        _httpClient = httpClient;
        _publicAppOptions = publicAppOptions.Value;
    }

    public async Task<AdminPlatformBillingStatusDto> GetStatusAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);
        var configuration = await GetConfigurationAsync(cancellationToken);
        return MapStatus(configuration);
    }

    public async Task<AdminPlatformBillingStatusDto> ConfigureAsync(WorkspaceSessionContext session, ConfigureAdminPlatformBillingRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateRootPasswordAsync(session, request.Password, cancellationToken);
        var accessToken = request.AccessToken?.Trim();
        if (string.IsNullOrWhiteSpace(accessToken) || accessToken.Length is < 40 or > 512)
            throw new ArgumentException("Informe um Access Token valido do Mercado Pago.", nameof(request.AccessToken));

        var account = await SendAsync<MercadoPagoAccount>(HttpMethod.Get, "users/me", accessToken, null, cancellationToken)
            ?? throw new InvalidOperationException("O Mercado Pago nao retornou os dados da conta.");
        if (account.Id is null)
            throw new InvalidOperationException("O Access Token nao identificou uma conta Mercado Pago valida.");

        var configuration = await GetConfigurationAsync(cancellationToken);
        var encryptedToken = _protector.Protect(accessToken);
        var liveMode = accessToken.StartsWith("APP_USR-", StringComparison.OrdinalIgnoreCase);
        if (configuration is null)
        {
            configuration = new PlatformBillingConfiguration(encryptedToken, account.Id.Value.ToString(), account.Email, liveMode, session.UserId);
            await _context.PlatformBillingConfigurations.AddAsync(configuration, cancellationToken);
        }
        else
        {
            configuration.Update(encryptedToken, account.Id.Value.ToString(), account.Email, liveMode, session.UserId);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapStatus(configuration);
    }

    public async Task DisconnectAsync(WorkspaceSessionContext session, AdminSensitiveActionRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateRootPasswordAsync(session, request.Password, cancellationToken);
        var configuration = await GetConfigurationAsync(cancellationToken);
        if (configuration is null) return;
        _context.PlatformBillingConfigurations.Remove(configuration);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminSubscriptionCheckoutDto> CreateSubscriptionCheckoutAsync(WorkspaceSessionContext session, Guid companyId, AdminSensitiveActionRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateRootPasswordAsync(session, request.Password, cancellationToken);
        var configuration = await GetRequiredConfigurationAsync(cancellationToken);
        var accessToken = Unprotect(configuration.AccessTokenCipherText);
        var company = await _context.Companies.FirstOrDefaultAsync(item => item.Id == companyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Empresa nao encontrada.");
        var ownerEmail = await _context.Users.Where(item => item.CompanyId == companyId && item.Role == UserRole.Owner)
            .OrderByDescending(item => item.IsActive).Select(item => item.Email).FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("A empresa nao possui owner para a cobranca.");
        var subscription = await GetSubscriptionAsync(company.TenantId, cancellationToken);
        if (subscription.MonthlyPrice <= 0) throw new InvalidOperationException("O plano precisa ter valor mensal maior que zero.");
        if (!string.IsNullOrWhiteSpace(subscription.MercadoPagoPreapprovalId))
            throw new InvalidOperationException("Essa empresa ja possui uma assinatura Mercado Pago. Sincronize o status em vez de gerar outra cobranca.");

        var payload = new
        {
            reason = $"{subscription.PlanName} - {company.TradeName}",
            external_reference = subscription.Id.ToString(),
            payer_email = ownerEmail,
            auto_recurring = new { frequency = 1, frequency_type = "months", transaction_amount = subscription.MonthlyPrice, currency_id = "BRL" },
            back_url = BuildBackUrl()
        };
        var response = await SendAsync<MercadoPagoPreapproval>(HttpMethod.Post, "preapproval", accessToken, payload, cancellationToken, subscription.Id.ToString())
            ?? throw new InvalidOperationException("O Mercado Pago nao retornou a assinatura criada.");
        if (string.IsNullOrWhiteSpace(response.Id) || string.IsNullOrWhiteSpace(response.InitPoint))
            throw new InvalidOperationException("O Mercado Pago nao retornou o link de pagamento da assinatura.");

        subscription.RegisterMercadoPagoCheckout(response.Id, response.InitPoint, response.Status ?? "pending", DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
        return MapCheckout(companyId, subscription);
    }

    public async Task<AdminSubscriptionCheckoutDto> SyncSubscriptionAsync(WorkspaceSessionContext session, Guid companyId, CancellationToken cancellationToken = default)
    {
        EnsureRoot(session);
        var configuration = await GetRequiredConfigurationAsync(cancellationToken);
        var company = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(item => item.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Empresa nao encontrada.");
        var subscription = await GetSubscriptionAsync(company.TenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(subscription.MercadoPagoPreapprovalId))
            throw new InvalidOperationException("Essa empresa ainda nao possui assinatura Mercado Pago.");
        var response = await SendAsync<MercadoPagoPreapproval>(HttpMethod.Get, $"preapproval/{Uri.EscapeDataString(subscription.MercadoPagoPreapprovalId)}", Unprotect(configuration.AccessTokenCipherText), null, cancellationToken)
            ?? throw new InvalidOperationException("Nao foi possivel consultar a assinatura no Mercado Pago.");
        subscription.UpdateMercadoPagoStatus(response.Status ?? "unknown", DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
        return MapCheckout(companyId, subscription);
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, string token, object? payload, CancellationToken cancellationToken, string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            request.Headers.TryAddWithoutValidation("X-Idempotency-Key", idempotencyKey);
        if (payload is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Mercado Pago recusou a operacao ({(int)response.StatusCode}). Confira a credencial e os dados da conta.");
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private async Task ValidateRootPasswordAsync(WorkspaceSessionContext session, string password, CancellationToken cancellationToken)
    {
        EnsureRoot(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == session.UserId, cancellationToken);
        if (user is null || user.Role != UserRole.Root || !_passwordHasher.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Senha root invalida.");
    }

    private static void EnsureRoot(WorkspaceSessionContext session)
    {
        if (!session.Role.Equals(UserRole.Root.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Apenas o root pode configurar cobrancas da plataforma.");
    }

    private Task<PlatformBillingConfiguration?> GetConfigurationAsync(CancellationToken cancellationToken) =>
        _context.PlatformBillingConfigurations.FirstOrDefaultAsync(item => item.Provider == Provider && item.IsActive, cancellationToken);

    private async Task<PlatformBillingConfiguration> GetRequiredConfigurationAsync(CancellationToken cancellationToken) =>
        await GetConfigurationAsync(cancellationToken) ?? throw new InvalidOperationException("Configure a conta Mercado Pago da plataforma primeiro.");

    private async Task<Subscription> GetSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken) =>
        await _context.Subscriptions.Where(item => item.TenantId == tenantId && item.IsActive)
            .OrderByDescending(item => item.StartsAtUtc).FirstOrDefaultAsync(cancellationToken)
        ?? throw new InvalidOperationException("A empresa nao possui plano ativo.");

    private string Unprotect(string value)
    {
        try { return _protector.Unprotect(value); }
        catch { throw new InvalidOperationException("A credencial salva nao pode ser lida neste servidor. Configure a conta novamente."); }
    }

    private string BuildBackUrl() => $"{(_publicAppOptions.BaseUrl ?? "https://zeropaperflow.com.br").TrimEnd('/')}/login";

    private static AdminPlatformBillingStatusDto MapStatus(PlatformBillingConfiguration? item) => new()
    {
        Configured = item is not null,
        AccountUserId = item?.AccountUserId,
        AccountEmail = item?.AccountEmail,
        LiveMode = item?.LiveMode ?? false,
        UpdatedAtUtc = item?.UpdatedAtUtc
    };

    private static AdminSubscriptionCheckoutDto MapCheckout(Guid companyId, Subscription item) => new()
    {
        CompanyId = companyId,
        SubscriptionId = item.Id,
        PlanName = item.PlanName,
        MonthlyPrice = item.MonthlyPrice,
        MercadoPagoStatus = item.MercadoPagoStatus,
        CheckoutUrl = item.MercadoPagoCheckoutUrl,
        StatusUpdatedAtUtc = item.MercadoPagoStatusUpdatedAtUtc
    };

    private sealed class MercadoPagoAccount { public long? Id { get; set; } public string? Email { get; set; } }
    private sealed class MercadoPagoPreapproval
    {
        public string? Id { get; set; }
        [JsonPropertyName("init_point")] public string? InitPoint { get; set; }
        public string? Status { get; set; }
    }
}
