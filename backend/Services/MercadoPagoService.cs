using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Domain.Enums;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class MercadoPagoService : IMercadoPagoService
{
    private const string TokenProtectorPurpose = "ZeroPaper.MercadoPago.Token.v1";
    private const string StateProtectorPurpose = "ZeroPaper.MercadoPago.OAuthState.v1";
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(15);

    private readonly ZeroPaperDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly MercadoPagoOptions _options;
    private readonly PublicAppOptions _publicAppOptions;
    private readonly IDataProtector _tokenProtector;
    private readonly ITimeLimitedDataProtector _stateProtector;
    private readonly ILogger<MercadoPagoService> _logger;

    public MercadoPagoService(
        ZeroPaperDbContext context,
        HttpClient httpClient,
        IOptions<MercadoPagoOptions> options,
        IOptions<PublicAppOptions> publicAppOptions,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<MercadoPagoService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _options = options.Value;
        _publicAppOptions = publicAppOptions.Value;
        _tokenProtector = dataProtectionProvider.CreateProtector(TokenProtectorPurpose);
        _stateProtector = dataProtectionProvider.CreateProtector(StateProtectorPurpose).ToTimeLimitedDataProtector();
        _logger = logger;
    }

    public async Task<MercadoPagoStatusDto> GetStatusAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        return new MercadoPagoStatusDto
        {
            Configured = IsPlatformConfigured(),
            Connected = company.IsMercadoPagoConnected,
            AccountUserId = company.MercadoPagoUserId,
            LiveMode = company.MercadoPagoLiveMode,
            ConnectedAtUtc = company.MercadoPagoConnectedAtUtc,
        };
    }

    public Task<MercadoPagoConnectResponseDto> StartConnectionAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!IsPlatformConfigured())
        {
            throw new InvalidOperationException("A integracao com o Mercado Pago ainda nao foi configurada na plataforma.");
        }

        var redirectUri = BuildRedirectUri();
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            throw new InvalidOperationException("O endereco publico da plataforma nao esta configurado.");
        }

        var state = _stateProtector.Protect(session.CompanyId.ToString("N"), StateLifetime);

        var authorizationUrl = string.Concat(
            ResolveAuthBaseUrl(),
            "authorization",
            "?client_id=", Uri.EscapeDataString(_options.ClientId!.Trim()),
            "&response_type=code",
            "&platform_id=mp",
            "&state=", Uri.EscapeDataString(state),
            "&redirect_uri=", Uri.EscapeDataString(redirectUri));

        return Task.FromResult(new MercadoPagoConnectResponseDto { AuthorizationUrl = authorizationUrl });
    }

    public async Task<bool> HandleOAuthCallbackAsync(string? state, string? code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(code) || !IsPlatformConfigured())
        {
            return false;
        }

        Guid companyId;
        try
        {
            var unprotected = _stateProtector.Unprotect(state);
            companyId = Guid.ParseExact(unprotected, "N");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Mercado Pago OAuth callback recebido com state invalido ou expirado.");
            return false;
        }

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == companyId, cancellationToken);

        if (company is null)
        {
            return false;
        }

        var token = await ExchangeAuthorizationCodeAsync(code, cancellationToken);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken) || token.UserId is null)
        {
            return false;
        }

        var expiresAtUtc = token.ExpiresIn > 0
            ? DateTime.UtcNow.AddSeconds(token.ExpiresIn)
            : (DateTime?)null;

        company.ConnectMercadoPago(
            token.UserId.Value.ToString(CultureInfo.InvariantCulture),
            _tokenProtector.Protect(token.AccessToken),
            string.IsNullOrWhiteSpace(token.RefreshToken) ? null : _tokenProtector.Protect(token.RefreshToken),
            token.PublicKey,
            token.LiveMode,
            expiresAtUtc,
            DateTime.UtcNow);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task DisconnectAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == session.CompanyId, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        if (!company.IsMercadoPagoConnected)
        {
            return;
        }

        company.DisconnectMercadoPago(DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<MercadoPagoCheckoutResponseDto> CreatePublicCheckoutAsync(
        string publicCode,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (!IsPlatformConfigured())
        {
            return new MercadoPagoCheckoutResponseDto
            {
                Available = false,
                Message = "Pagamento online ainda nao configurado na plataforma."
            };
        }

        var order = await _context.CustomerOrders
            .Include(item => item.Company)
            .Include(item => item.DiningTable)
                .ThenInclude(table => table.QrCodeAccess)
            .Include(item => item.Items)
            .FirstOrDefaultAsync(
                item =>
                    item.Id == orderId &&
                    item.IsActive &&
                    item.DiningTable.QrCodeAccess.PublicCode == publicCode,
                cancellationToken)
            ?? throw new KeyNotFoundException("Pedido nao encontrado.");

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            return new MercadoPagoCheckoutResponseDto
            {
                Available = false,
                Message = "Este pedido ja esta marcado como pago."
            };
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return new MercadoPagoCheckoutResponseDto
            {
                Available = false,
                Message = "Pedido cancelado nao pode ser pago online."
            };
        }

        if (!order.Company.IsMercadoPagoConnected)
        {
            return new MercadoPagoCheckoutResponseDto
            {
                Available = false,
                Message = "A unidade ainda nao conectou a conta Mercado Pago."
            };
        }

        var accessToken = await GetValidAccessTokenAsync(order.Company, cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return new MercadoPagoCheckoutResponseDto
            {
                Available = false,
                Message = "Nao foi possivel usar a conexao Mercado Pago desta unidade."
            };
        }

        var preference = await CreatePreferenceAsync(order, accessToken, publicCode, cancellationToken);
        if (preference is null)
        {
            return new MercadoPagoCheckoutResponseDto
            {
                Available = false,
                Message = "Nao foi possivel iniciar o pagamento online agora."
            };
        }

        var initPoint = order.Company.MercadoPagoLiveMode
            ? preference.InitPoint
            : preference.SandboxInitPoint ?? preference.InitPoint;

        return new MercadoPagoCheckoutResponseDto
        {
            Available = !string.IsNullOrWhiteSpace(initPoint),
            InitPoint = initPoint,
            PreferenceId = preference.Id,
            Message = string.IsNullOrWhiteSpace(initPoint)
                ? "Mercado Pago nao retornou um link de pagamento."
                : "Pagamento online pronto."
        };
    }

    public async Task<bool> HandlePaymentNotificationAsync(string? paymentId, Guid? orderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentId) || !orderId.HasValue)
        {
            return false;
        }

        var order = await _context.CustomerOrders
            .Include(item => item.Company)
            .Include(item => item.Payments)
            .FirstOrDefaultAsync(item => item.Id == orderId.Value && item.IsActive, cancellationToken);

        if (order is null || order.Status == OrderStatus.Cancelled)
        {
            return false;
        }

        var accessToken = await GetValidAccessTokenAsync(order.Company, cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        var payment = await GetPaymentAsync(paymentId, accessToken, cancellationToken);
        if (payment is null ||
            !string.Equals(payment.Status, "approved", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(payment.ExternalReference) ||
            !Guid.TryParse(payment.ExternalReference, out var paymentOrderId) ||
            paymentOrderId != order.Id)
        {
            return false;
        }

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            return true;
        }

        var method = MapMercadoPagoPaymentMethod(payment.PaymentMethodId, payment.PaymentTypeId);
        order.ReplacePayments([
            new CustomerOrderPayment(order.TenantId, order.Id, method, order.TotalAmount)
        ]);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<MercadoPagoTokenResponse?> ExchangeAuthorizationCodeAsync(string code, CancellationToken cancellationToken)
    {
        var redirectUri = BuildRedirectUri();
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return null;
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _options.ClientId!.Trim(),
            ["client_secret"] = _options.ClientSecret!.Trim(),
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
        });

        try
        {
            using var response = await _httpClient.PostAsync("oauth/token", content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Falha ao trocar o codigo do Mercado Pago. Status {Status}. Corpo: {Body}",
                    (int)response.StatusCode,
                    payload);
                return null;
            }

            return JsonSerializer.Deserialize<MercadoPagoTokenResponse>(payload);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro inesperado ao trocar o codigo OAuth do Mercado Pago.");
            return null;
        }
    }

    private async Task<string?> GetValidAccessTokenAsync(Company company, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(company.MercadoPagoAccessTokenCipherText))
        {
            return null;
        }

        var currentToken = UnprotectOrNull(company.MercadoPagoAccessTokenCipherText);
        if (currentToken is null)
        {
            return null;
        }

        if (!company.MercadoPagoTokenExpiresAtUtc.HasValue ||
            company.MercadoPagoTokenExpiresAtUtc.Value > DateTime.UtcNow.AddDays(7) ||
            string.IsNullOrWhiteSpace(company.MercadoPagoRefreshTokenCipherText))
        {
            return currentToken;
        }

        var refreshToken = UnprotectOrNull(company.MercadoPagoRefreshTokenCipherText);
        if (refreshToken is null)
        {
            return currentToken;
        }

        var refreshed = await RefreshAccessTokenAsync(refreshToken, cancellationToken);
        if (refreshed is null || string.IsNullOrWhiteSpace(refreshed.AccessToken))
        {
            return currentToken;
        }

        var expiresAtUtc = refreshed.ExpiresIn > 0
            ? DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn)
            : (DateTime?)null;

        company.UpdateMercadoPagoTokens(
            _tokenProtector.Protect(refreshed.AccessToken),
            string.IsNullOrWhiteSpace(refreshed.RefreshToken)
                ? company.MercadoPagoRefreshTokenCipherText
                : _tokenProtector.Protect(refreshed.RefreshToken),
            expiresAtUtc);

        await _context.SaveChangesAsync(cancellationToken);
        return refreshed.AccessToken;
    }

    private async Task<MercadoPagoTokenResponse?> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.ClientId!.Trim(),
            ["client_secret"] = _options.ClientSecret!.Trim(),
            ["refresh_token"] = refreshToken,
        });

        try
        {
            using var response = await _httpClient.PostAsync("oauth/token", content, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Falha ao renovar token Mercado Pago. Status {Status}. Corpo: {Body}",
                    (int)response.StatusCode,
                    payload);
                return null;
            }

            return JsonSerializer.Deserialize<MercadoPagoTokenResponse>(payload);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro inesperado ao renovar token OAuth do Mercado Pago.");
            return null;
        }
    }

    private async Task<MercadoPagoPreferenceResponse?> CreatePreferenceAsync(
        CustomerOrder order,
        string accessToken,
        string publicCode,
        CancellationToken cancellationToken)
    {
        var publicBaseUrl = ResolvePublicBaseUrl();
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            return null;
        }

        var orderTitle = $"Pedido #{order.Number} - {order.Company.TradeName}";
        var payload = new
        {
            items = new[]
            {
                new
                {
                    title = orderTitle,
                    quantity = 1,
                    currency_id = "BRL",
                    unit_price = order.TotalAmount
                }
            },
            external_reference = order.Id.ToString(),
            notification_url = $"{publicBaseUrl.TrimEnd('/')}/api/public/payments/mercadopago/webhook?orderId={Uri.EscapeDataString(order.Id.ToString())}",
            back_urls = new
            {
                success = $"{publicBaseUrl.TrimEnd('/')}/q/{Uri.EscapeDataString(publicCode)}?mp=success",
                pending = $"{publicBaseUrl.TrimEnd('/')}/q/{Uri.EscapeDataString(publicCode)}?mp=pending",
                failure = $"{publicBaseUrl.TrimEnd('/')}/q/{Uri.EscapeDataString(publicCode)}?mp=failure"
            },
            auto_return = "approved",
            payment_methods = new
            {
                excluded_payment_types = new[]
                {
                    new { id = "ticket" },
                    new { id = "atm" }
                },
                installments = 6
            },
            metadata = new
            {
                company_id = order.CompanyId.ToString(),
                order_id = order.Id.ToString()
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "checkout/preferences")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responsePayload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Falha ao criar preferencia Mercado Pago para pedido {OrderId}. Status {Status}. Corpo: {Body}",
                    order.Id,
                    (int)response.StatusCode,
                    responsePayload);
                return null;
            }

            return JsonSerializer.Deserialize<MercadoPagoPreferenceResponse>(responsePayload);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro inesperado ao criar preferencia Mercado Pago para pedido {OrderId}.", order.Id);
            return null;
        }
    }

    private async Task<MercadoPagoPaymentResponse?> GetPaymentAsync(string paymentId, string accessToken, CancellationToken cancellationToken)
    {
        if (!long.TryParse(paymentId, out _))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"v1/payments/{Uri.EscapeDataString(paymentId)}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Falha ao consultar pagamento Mercado Pago {PaymentId}. Status {Status}. Corpo: {Body}",
                    paymentId,
                    (int)response.StatusCode,
                    payload);
                return null;
            }

            return JsonSerializer.Deserialize<MercadoPagoPaymentResponse>(payload);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro inesperado ao consultar pagamento Mercado Pago {PaymentId}.", paymentId);
            return null;
        }
    }

    private string? UnprotectOrNull(string? cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
        {
            return null;
        }

        try
        {
            return _tokenProtector.Unprotect(cipherText);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Nao foi possivel descriptografar token Mercado Pago.");
            return null;
        }
    }

    private static PaymentMethod MapMercadoPagoPaymentMethod(string? paymentMethodId, string? paymentTypeId)
    {
        if (string.Equals(paymentMethodId, "pix", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentMethod.Pix;
        }

        return paymentTypeId?.ToLowerInvariant() switch
        {
            "credit_card" => PaymentMethod.Credit,
            "debit_card" => PaymentMethod.Debit,
            "bank_transfer" => PaymentMethod.Pix,
            "account_money" => PaymentMethod.Pix,
            _ => PaymentMethod.Pix
        };
    }

    private bool IsPlatformConfigured()
    {
        return !string.IsNullOrWhiteSpace(_options.ClientId) && !string.IsNullOrWhiteSpace(_options.ClientSecret);
    }

    private string ResolveAuthBaseUrl()
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.AuthBaseUrl)
            ? MercadoPagoOptions.DefaultAuthBaseUrl
            : _options.AuthBaseUrl.Trim();

        return baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
    }

    private string? BuildRedirectUri()
    {
        var baseUrl = ResolvePublicBaseUrl();
        return string.IsNullOrWhiteSpace(baseUrl)
            ? null
            : $"{baseUrl.TrimEnd('/')}/api/workspace/payments/mercadopago/callback";
    }

    private string? ResolvePublicBaseUrl()
    {
        var configured = Environment.GetEnvironmentVariable("PUBLIC_APP_BASE_URL")
            ?? _publicAppOptions.BaseUrl;

        return string.IsNullOrWhiteSpace(configured) ? null : configured.Trim();
    }

    private sealed class MercadoPagoTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("public_key")]
        public string? PublicKey { get; set; }

        [JsonPropertyName("user_id")]
        public long? UserId { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("live_mode")]
        public bool LiveMode { get; set; }
    }

    private sealed class MercadoPagoPreferenceResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("init_point")]
        public string? InitPoint { get; set; }

        [JsonPropertyName("sandbox_init_point")]
        public string? SandboxInitPoint { get; set; }
    }

    private sealed class MercadoPagoPaymentResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("external_reference")]
        public string? ExternalReference { get; set; }

        [JsonPropertyName("payment_method_id")]
        public string? PaymentMethodId { get; set; }

        [JsonPropertyName("payment_type_id")]
        public string? PaymentTypeId { get; set; }
    }
}
