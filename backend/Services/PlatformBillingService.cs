using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
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
    private readonly IBillingNotificationService _billingNotificationService;
    private readonly ILogger<PlatformBillingService> _logger;

    public PlatformBillingService(
        ZeroPaperDbContext context,
        IPasswordHasher passwordHasher,
        IDataProtectionProvider dataProtectionProvider,
        HttpClient httpClient,
        IOptions<PublicAppOptions> publicAppOptions,
        IBillingNotificationService billingNotificationService,
        ILogger<PlatformBillingService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _protector = dataProtectionProvider.CreateProtector("ZeroPaper.PlatformBilling.MercadoPago.AccessToken.v1");
        _httpClient = httpClient;
        _publicAppOptions = publicAppOptions.Value;
        _billingNotificationService = billingNotificationService;
        _logger = logger;
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
        // Mercado Pago test users can also expose tokens with the APP_USR prefix.
        // The account returned by /users/me is the source of truth for identifying
        // the well-known test-user domain; relying on the token prefix alone caused
        // production checkouts to mix a test collector with a real payer.
        var liveMode = accessToken.StartsWith("APP_USR-", StringComparison.OrdinalIgnoreCase) &&
            !IsTestAccountEmail(account.Email);
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
        EnsureProductionCheckoutAccount(configuration, ownerEmail);
        var subscription = await GetSubscriptionAsync(company.TenantId, cancellationToken);
        if (subscription.MonthlyPrice <= 0) throw new InvalidOperationException("O plano precisa ter valor mensal maior que zero.");
        if (!string.IsNullOrWhiteSpace(subscription.MercadoPagoPreapprovalId) || !string.IsNullOrWhiteSpace(subscription.MercadoPagoPreapprovalPlanId))
            throw new InvalidOperationException("Essa empresa ja possui uma assinatura Mercado Pago. Sincronize o status em vez de gerar outra cobranca.");

        var payload = new
        {
            reason = $"{subscription.PlanName} - {company.TradeName}",
            external_reference = subscription.Id.ToString(),
            auto_recurring = new { frequency = 1, frequency_type = "months", transaction_amount = subscription.MonthlyPrice, currency_id = "BRL" },
            back_url = BuildBackUrl()
        };
        var response = await SendAsync<MercadoPagoPreapproval>(HttpMethod.Post, "preapproval_plan", accessToken, payload, cancellationToken, subscription.Id.ToString())
            ?? throw new InvalidOperationException("O Mercado Pago nao retornou a assinatura criada.");
        if (string.IsNullOrWhiteSpace(response.Id) || string.IsNullOrWhiteSpace(response.InitPoint))
            throw new InvalidOperationException("O Mercado Pago nao retornou o link de pagamento da assinatura.");

        subscription.RegisterMercadoPagoPlanCheckout(response.Id, response.InitPoint, response.Status ?? "active", DateTime.UtcNow);
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
        await ResolvePreapprovalFromPlanAsync(subscription, configuration, cancellationToken);
        if (string.IsNullOrWhiteSpace(subscription.MercadoPagoPreapprovalId))
            throw new InvalidOperationException("Essa empresa ainda nao possui assinatura Mercado Pago.");
        var response = await SendAsync<MercadoPagoPreapproval>(HttpMethod.Get, $"preapproval/{Uri.EscapeDataString(subscription.MercadoPagoPreapprovalId)}", Unprotect(configuration.AccessTokenCipherText), null, cancellationToken)
            ?? throw new InvalidOperationException("Nao foi possivel consultar a assinatura no Mercado Pago.");
        subscription.UpdateMercadoPagoStatus(response.Status ?? "unknown", DateTime.UtcNow);
        await ProcessApprovedPaymentsAsync(company, subscription, configuration, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return MapCheckout(companyId, subscription);
    }

    public async Task<AdminSubscriptionCheckoutDto> CreateSignupCheckoutAsync(Guid subscriptionId, Guid companyId, string ownerEmail, CancellationToken cancellationToken = default)
    {
        var configuration = await GetRequiredConfigurationAsync(cancellationToken);
        EnsureProductionCheckoutAccount(configuration, ownerEmail);
        var company = await _context.Companies.FirstOrDefaultAsync(item => item.Id == companyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Empresa nao encontrada.");
        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(item => item.Id == subscriptionId && item.TenantId == company.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Plano nao encontrado.");
        if (!string.IsNullOrWhiteSpace(subscription.MercadoPagoPreapprovalId) || !string.IsNullOrWhiteSpace(subscription.MercadoPagoPreapprovalPlanId)) return MapCheckout(companyId, subscription);
        var rawConfirmationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        subscription.SetCheckoutConfirmationTokenHash(ComputeTokenHash(rawConfirmationToken));
        var payload = new
        {
            reason = $"{subscription.PlanName} - {company.TradeName}", external_reference = subscription.Id.ToString(),
            auto_recurring = new { frequency = 1, frequency_type = "months", transaction_amount = subscription.MonthlyPrice, currency_id = "BRL" },
            back_url = $"{GetPublicBaseUrl()}/cadastro/confirmacao?pagamento={rawConfirmationToken}"
        };
        var response = await SendAsync<MercadoPagoPreapproval>(HttpMethod.Post, "preapproval_plan", Unprotect(configuration.AccessTokenCipherText), payload, cancellationToken, subscription.Id.ToString())
            ?? throw new InvalidOperationException("O Mercado Pago nao retornou a assinatura criada.");
        if (string.IsNullOrWhiteSpace(response.Id) || string.IsNullOrWhiteSpace(response.InitPoint)) throw new InvalidOperationException("O Mercado Pago nao retornou o checkout.");
        subscription.RegisterMercadoPagoPlanCheckout(response.Id, response.InitPoint, response.Status ?? "active", DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
        return MapCheckout(companyId, subscription);
    }

    public async Task<SubscriptionPaymentConfirmationDto> ConfirmSignupPaymentAsync(string confirmationToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(confirmationToken) || confirmationToken.Length != 64) throw new ArgumentException("Confirmacao invalida.");
        var hash = ComputeTokenHash(confirmationToken);
        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(item => item.CheckoutConfirmationTokenHash == hash && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Confirmacao nao encontrada.");
        var company = await _context.Companies.FirstAsync(item => item.TenantId == subscription.TenantId && item.IsActive, cancellationToken);
        var configuration = await GetRequiredConfigurationAsync(cancellationToken);
        await ResolvePreapprovalFromPlanAsync(subscription, configuration, cancellationToken);
        var paid = await ProcessApprovedPaymentsAsync(company, subscription, configuration, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return BuildConfirmation(subscription, paid);
    }

    public async Task<SubscriptionPaymentConfirmationDto> MarkManualPaymentAsync(WorkspaceSessionContext session, Guid companyId, AdminSensitiveActionRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateRootPasswordAsync(session, request.Password, cancellationToken);
        var company = await _context.Companies.FirstOrDefaultAsync(item => item.Id == companyId && item.IsActive, cancellationToken) ?? throw new KeyNotFoundException("Empresa nao encontrada.");
        var subscription = await GetSubscriptionAsync(company.TenantId, cancellationToken);
        var owner = await _context.Users.FirstAsync(item => item.CompanyId == companyId && item.Role == UserRole.Owner, cancellationToken);
        var payment = new SubscriptionPayment(company.TenantId, subscription.Id, "manual", Guid.NewGuid().ToString("N"), subscription.MonthlyPrice, DateTime.UtcNow, session.UserId);
        subscription.RegisterPaidMonth(payment.PaidAtUtc); owner.Activate();
        await _context.SubscriptionPayments.AddAsync(payment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await NotifySafelyAsync(company, owner, subscription, payment, cancellationToken);
        return BuildConfirmation(subscription, true);
    }

    public async Task<DateTime?> RefreshTenantPaidAccessAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var subscription = await _context.Subscriptions.Where(item => item.TenantId == tenantId && item.IsActive)
            .OrderByDescending(item => item.StartsAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (subscription is null || string.IsNullOrWhiteSpace(subscription.MercadoPagoPreapprovalId)) return subscription?.PaidThroughUtc;
        var configuration = await GetConfigurationAsync(cancellationToken);
        if (configuration is null) return subscription.PaidThroughUtc;
        var company = await _context.Companies.FirstAsync(item => item.TenantId == tenantId && item.IsActive, cancellationToken);
        await ResolvePreapprovalFromPlanAsync(subscription, configuration, cancellationToken);
        await ProcessApprovedPaymentsAsync(company, subscription, configuration, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return subscription.PaidThroughUtc;
    }

    private async Task<bool> ProcessApprovedPaymentsAsync(Company company, Subscription subscription, PlatformBillingConfiguration configuration, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subscription.MercadoPagoPreapprovalId)) return false;
        var search = await SendAsync<AuthorizedPaymentSearch>(HttpMethod.Get, $"authorized_payments/search?preapproval_id={Uri.EscapeDataString(subscription.MercadoPagoPreapprovalId)}", Unprotect(configuration.AccessTokenCipherText), null, cancellationToken);
        var owner = await _context.Users.FirstAsync(item => item.CompanyId == company.Id && item.Role == UserRole.Owner, cancellationToken);
        var activated = false;
        foreach (var invoice in (search?.Results ?? []).OrderBy(item => item.DebitDate ?? item.DateCreated ?? DateTime.MaxValue))
        {
            if (!string.Equals(invoice.Payment?.Status, "approved", StringComparison.OrdinalIgnoreCase) || invoice.Payment?.Id is null) continue;
            var externalId = invoice.Payment.Id.Value.ToString();
            if (await _context.SubscriptionPayments.AnyAsync(item => item.Source == "mercadopago" && item.ExternalPaymentId == externalId, cancellationToken)) continue;
            if (invoice.TransactionAmount < subscription.MonthlyPrice || !string.Equals(invoice.CurrencyId, "BRL", StringComparison.OrdinalIgnoreCase)) continue;
            var paidAt = invoice.DebitDate ?? invoice.DateCreated ?? DateTime.UtcNow;
            var payment = new SubscriptionPayment(company.TenantId, subscription.Id, "mercadopago", externalId, invoice.TransactionAmount, paidAt);
            subscription.RegisterPaidMonth(paidAt); owner.Activate();
            await _context.SubscriptionPayments.AddAsync(payment, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await NotifySafelyAsync(company, owner, subscription, payment, cancellationToken);
            activated = true;
        }
        return activated || subscription.HasPaidAccess(DateTime.UtcNow);
    }

    private async Task ResolvePreapprovalFromPlanAsync(Subscription subscription, PlatformBillingConfiguration configuration, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(subscription.MercadoPagoPreapprovalId) || string.IsNullOrWhiteSpace(subscription.MercadoPagoPreapprovalPlanId)) return;
        var planId = Uri.EscapeDataString(subscription.MercadoPagoPreapprovalPlanId);
        var search = await SendAsync<PreapprovalSearch>(HttpMethod.Get, $"preapproval/search?preapproval_plan_id={planId}", Unprotect(configuration.AccessTokenCipherText), null, cancellationToken);
        var match = search?.Results.FirstOrDefault(item =>
            !string.IsNullOrWhiteSpace(item.Id) &&
            string.Equals(item.PreapprovalPlanId, subscription.MercadoPagoPreapprovalPlanId, StringComparison.Ordinal));
        if (match is null) return;
        subscription.RegisterMercadoPagoCheckout(match.Id!, subscription.MercadoPagoCheckoutUrl!, match.Status ?? "pending", DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifySafelyAsync(Company company, AppUser owner, Subscription subscription, SubscriptionPayment payment, CancellationToken cancellationToken)
    {
        try { await _billingNotificationService.SendPaymentConfirmedAsync(company, owner, subscription, payment, cancellationToken); }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Payment {PaymentId} was applied, but its billing notification email failed.", payment.ExternalPaymentId);
        }
    }

    private static SubscriptionPaymentConfirmationDto BuildConfirmation(Subscription subscription, bool paid) => new()
    {
        Paid = paid, AccessActive = subscription.HasPaidAccess(DateTime.UtcNow), PaidThroughUtc = subscription.PaidThroughUtc,
        Message = paid ? "Pagamento confirmado. Seu acesso esta liberado." : "Pagamento ainda em processamento. Tente novamente em alguns instantes."
    };

    private string GetPublicBaseUrl() => (_publicAppOptions.BaseUrl ?? "https://zeropaperflow.com.br").TrimEnd('/');
    private static string ComputeTokenHash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static void EnsureProductionCheckoutAccount(PlatformBillingConfiguration configuration, string payerEmail)
    {
        if (!configuration.LiveMode || IsTestAccountEmail(configuration.AccountEmail))
        {
            throw new InvalidOperationException(
                "O Mercado Pago da plataforma esta conectado a uma conta de teste. " +
                "Configure no painel root o Access Token de producao de uma conta real antes de receber pagamentos.");
        }

        if (!string.IsNullOrWhiteSpace(configuration.AccountEmail) &&
            string.Equals(configuration.AccountEmail.Trim(), payerEmail.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A conta que recebe nao pode pagar a propria assinatura. Use outra conta ou outro meio de pagamento.");
        }
    }

    private static bool IsTestAccountEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) &&
        email.Trim().EndsWith("@testuser.com", StringComparison.OrdinalIgnoreCase);

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
        {
            var providerError = ExtractProviderError(body);
            _logger.LogWarning(
                "Mercado Pago rejected {Method} {Path} with status {StatusCode}. Provider error: {ProviderError}",
                method.Method,
                path,
                (int)response.StatusCode,
                providerError);
            if (providerError.Contains("Both payer and collector must be real or test users", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "O Mercado Pago recusou o pagamento porque a conta recebedora e o pagador pertencem a ambientes diferentes (teste e producao). " +
                    "Configure uma conta real para receber pagamentos reais.");
            }

            throw new InvalidOperationException($"Mercado Pago recusou a operacao ({(int)response.StatusCode}). Confira a credencial e os dados da conta.");
        }
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static string ExtractProviderError(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var parts = new List<string>();
            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                parts.Add(message.GetString()!);
            if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                parts.Add(error.GetString()!);
            if (root.TryGetProperty("cause", out var causes) && causes.ValueKind == JsonValueKind.Array)
            {
                foreach (var cause in causes.EnumerateArray().Take(3))
                {
                    if (cause.TryGetProperty("code", out var code)) parts.Add($"code={code}");
                    if (cause.TryGetProperty("description", out var description) && description.ValueKind == JsonValueKind.String)
                        parts.Add(description.GetString()!);
                }
            }

            if (parts.Count == 0) return "unspecified";
            var result = string.Join(" | ", parts).ReplaceLineEndings(" ");
            return result[..Math.Min(result.Length, 500)];
        }
        catch (JsonException)
        {
            return "unparseable response";
        }
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
        LiveMode = item is not null && item.LiveMode && !IsTestAccountEmail(item.AccountEmail),
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
    private sealed class PreapprovalSearch { public List<PreapprovalSearchItem> Results { get; set; } = []; }
    private sealed class PreapprovalSearchItem
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        [JsonPropertyName("preapproval_plan_id")] public string? PreapprovalPlanId { get; set; }
    }
    private sealed class AuthorizedPaymentSearch { public List<AuthorizedPayment> Results { get; set; } = []; }
    private sealed class AuthorizedPayment
    {
        [JsonPropertyName("transaction_amount"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] public decimal TransactionAmount { get; set; }
        [JsonPropertyName("currency_id")] public string? CurrencyId { get; set; }
        [JsonPropertyName("debit_date")] public DateTime? DebitDate { get; set; }
        [JsonPropertyName("date_created")] public DateTime? DateCreated { get; set; }
        public AuthorizedPaymentDetail? Payment { get; set; }
    }
    private sealed class AuthorizedPaymentDetail { public long? Id { get; set; } public string? Status { get; set; } }
}
