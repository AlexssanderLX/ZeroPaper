using System.Globalization;
using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public class Company : TenantOwnedEntity
{
    private const string DefaultAiAssistantModel = "gpt-5.4-mini";
    private const string DefaultAiAssistantSystemPrompt =
        "Atue como atendente digital do ZeroPaper em portugues do Brasil, com estilo de delivery moderno: gentil, animado, " +
        "humano, rapido e facil de conversar. Use emojis leves e relevantes, normalmente 1 a 3 por resposta, sem exagero. " +
        "Responda primeiro a duvida do cliente de forma util e so depois conduza para o fluxo oficial quando fizer sentido. " +
        "Nao invente itens, valores, disponibilidade ou prazos. Oriente para o sistema sempre que for necessario fechar pedido, " +
        "confirmar endereco, nome ou pagamento. Nao repita boas-vindas, apresentacao ou links sem necessidade.";
    private const string DefaultAiAssistantGreetingMessage =
        "Ola! Seja bem-vindo 😊 Posso te ajudar com duvidas, cardapio, delivery e te encaminhar para o pedido oficial da unidade.";
    private const string DefaultAiAssistantRedirectMessage =
        "Perfeito! Para escolher os itens e finalizar com seguranca, use o link oficial da unidade no ZeroPaper 👇";
    private const string DefaultAiAssistantFallbackMessage =
        "Boa pergunta 😊 Para evitar te passar algo errado, siga pelo canal oficial da unidade ou me diga como posso te ajudar com o pedido.";

    private readonly List<AppUser> _users = [];
    private readonly List<AppSession> _sessions = [];
    private readonly List<QrCodeAccess> _qrCodeAccesses = [];
    private readonly List<DiningTable> _tables = [];
    private readonly List<CustomerOrder> _orders = [];
    private readonly List<WaiterCall> _waiterCalls = [];
    private readonly List<StockItem> _stockItems = [];
    private readonly List<MenuCategory> _menuCategories = [];
    private readonly List<MenuItem> _menuItems = [];
    private readonly List<WhatsAppConversation> _whatsAppConversations = [];

    private Company()
    {
    }

    public Company(
        Guid tenantId,
        string legalName,
        string tradeName,
        string accessSlug,
        string? documentNumber = null,
        string? contactEmail = null,
        string? contactPhone = null) : base(tenantId)
    {
        UpdateNames(legalName, tradeName);
        ChangeAccessSlug(accessSlug);
        UpdateContact(contactEmail, contactPhone);
        DocumentNumber = documentNumber?.Trim();
    }

    public string LegalName { get; private set; } = null!;
    public string TradeName { get; private set; } = null!;
    public string? LogoUrl { get; private set; }
    public string AccessSlug { get; private set; } = null!;
    public string? DocumentNumber { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public int LastOrderNumber { get; private set; }
    public bool EnableOrderAlerts { get; private set; } = true;
    public bool EnableWaiterCallAlerts { get; private set; } = true;
    public string? AlertSoundUrl { get; private set; }
    public int AlertVolumePercent { get; private set; } = 100;
    public int AlertPlaybackSeconds { get; private set; } = 6;
    public bool EnableAutomaticPrinting { get; private set; } = true;
    public PrintPaperProfile PrintPaperProfile { get; private set; } = PrintPaperProfile.Thermal80mm;
    public int PrintOrdersPerPage { get; private set; } = 1;
    public string? PrintAgentKeyHash { get; private set; }
    public string? PrintAgentName { get; private set; }
    public string? PrintAgentPrinterName { get; private set; }
    public DateTime? PrintAgentLastSeenAtUtc { get; private set; }
    public bool EnableAiAssistant { get; private set; }
    public string AiAssistantModel { get; private set; } = DefaultAiAssistantModel;
    public string AiAssistantSystemPrompt { get; private set; } = DefaultAiAssistantSystemPrompt;
    public string AiAssistantGreetingMessage { get; private set; } = DefaultAiAssistantGreetingMessage;
    public string AiAssistantRedirectMessage { get; private set; } = DefaultAiAssistantRedirectMessage;
    public string AiAssistantFallbackMessage { get; private set; } = DefaultAiAssistantFallbackMessage;
    public string? AiAssistantOrderingLink { get; private set; }
    public string? AiAssistantPixReceiverName { get; private set; }
    public string? AiAssistantPixKey { get; private set; }
    public string? AiAssistantPixMessage { get; private set; }
    public string? AiAssistantServiceDays { get; private set; }
    public string? AiAssistantServiceStartTime { get; private set; }
    public string? AiAssistantServiceEndTime { get; private set; }
    public int AiAssistantMaxOutputTokens { get; private set; } = 300;
    public bool EnableWhatsAppAssistant { get; private set; }
    public string? WhatsAppInstanceId { get; private set; }
    public string? WhatsAppInstanceTokenCipherText { get; private set; }
    public string? WhatsAppAccountSecurityTokenCipherText { get; private set; }
    public string? WhatsAppWebhookSecretCipherText { get; private set; }
    public bool IsWhatsAppConnected { get; private set; }
    public string? WhatsAppConnectedPhone { get; private set; }
    public DateTime? WhatsAppConnectedAtUtc { get; private set; }
    public DateTime? WhatsAppDisconnectedAtUtc { get; private set; }
    public DateTime? WhatsAppLastIncomingAtUtc { get; private set; }
    public DateTime? WhatsAppLastOutgoingAtUtc { get; private set; }
    public bool EnableDeliveryFreight { get; private set; }
    public string? DeliveryOriginPostalCode { get; private set; }
    public decimal DeliveryFreightPricePerKm { get; private set; }
    public decimal DeliveryFreightBaseFee { get; private set; }
    public decimal DeliveryFreightBaseDistanceKm { get; private set; }
    public int? PickupEstimatedMinutes { get; private set; }
    public int? DeliveryEstimatedMinutes { get; private set; }
    public string? AdminMasterPasswordHash { get; private set; }
    public string? AdminMasterPasswordCipherText { get; private set; }
    public DateTime? AdminMasterPasswordRotatedAtUtc { get; private set; }
    public string? MercadoPagoUserId { get; private set; }
    public string? MercadoPagoAccessTokenCipherText { get; private set; }
    public string? MercadoPagoRefreshTokenCipherText { get; private set; }
    public string? MercadoPagoPublicKey { get; private set; }
    public bool MercadoPagoLiveMode { get; private set; }
    public DateTime? MercadoPagoTokenExpiresAtUtc { get; private set; }
    public bool IsMercadoPagoConnected { get; private set; }
    public DateTime? MercadoPagoConnectedAtUtc { get; private set; }
    public DateTime? MercadoPagoDisconnectedAtUtc { get; private set; }
    public string TimeZoneId { get; private set; } = "America/Sao_Paulo";

    public Tenant Tenant { get; private set; } = null!;
    public IReadOnlyCollection<AppUser> Users => _users.AsReadOnly();
    public IReadOnlyCollection<AppSession> Sessions => _sessions.AsReadOnly();
    public IReadOnlyCollection<QrCodeAccess> QrCodeAccesses => _qrCodeAccesses.AsReadOnly();
    public IReadOnlyCollection<DiningTable> Tables => _tables.AsReadOnly();
    public IReadOnlyCollection<CustomerOrder> Orders => _orders.AsReadOnly();
    public IReadOnlyCollection<WaiterCall> WaiterCalls => _waiterCalls.AsReadOnly();
    public IReadOnlyCollection<StockItem> StockItems => _stockItems.AsReadOnly();
    public IReadOnlyCollection<MenuCategory> MenuCategories => _menuCategories.AsReadOnly();
    public IReadOnlyCollection<MenuItem> MenuItems => _menuItems.AsReadOnly();
    public IReadOnlyCollection<WhatsAppConversation> WhatsAppConversations => _whatsAppConversations.AsReadOnly();

    public void UpdateNames(string legalName, string tradeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tradeName);

        LegalName = legalName.Trim();
        TradeName = tradeName.Trim();
        Touch();
    }

    public void ChangeAccessSlug(string accessSlug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessSlug);
        AccessSlug = accessSlug.Trim().ToLowerInvariant();
        Touch();
    }

    public void UpdateContact(string? contactEmail, string? contactPhone)
    {
        ContactEmail = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail.Trim().ToLowerInvariant();
        ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim();
        Touch();
    }

    public void UpdateTimeZone(string timeZoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException("O fuso horário informado não existe.", nameof(timeZoneId));
        }
        catch (InvalidTimeZoneException)
        {
            throw new ArgumentException("O fuso horário informado não existe.", nameof(timeZoneId));
        }
    
        TimeZoneId = timeZoneId.Trim();
        Touch();
    }



    public void UpdateLogo(string? logoUrl)
    {
        LogoUrl = NormalizeOptionalValue(logoUrl, 500, nameof(logoUrl));
        Touch();
    }

    public int ReserveNextOrderNumber()
    {
        LastOrderNumber += 1;
        Touch();
        return LastOrderNumber;
    }

    public void ResetOrderNumberSequence()
    {
        LastOrderNumber = 0;
        Touch();
    }

    public void UpdateAlertPreferences(bool enableOrderAlerts, bool enableWaiterCallAlerts, int alertVolumePercent, int alertPlaybackSeconds)
    {
        if (alertVolumePercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(alertVolumePercent), "O volume precisa ficar entre 0 e 100.");
        }

        if (alertPlaybackSeconds is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(alertPlaybackSeconds), "A duracao precisa ficar entre 1 e 20 segundos.");
        }

        EnableOrderAlerts = enableOrderAlerts;
        EnableWaiterCallAlerts = enableWaiterCallAlerts;
        AlertVolumePercent = alertVolumePercent;
        AlertPlaybackSeconds = alertPlaybackSeconds;
        Touch();
    }

    public void UpdateAlertSound(string? alertSoundUrl)
    {
        AlertSoundUrl = string.IsNullOrWhiteSpace(alertSoundUrl) ? null : alertSoundUrl.Trim();
        Touch();
    }

    public void UpdatePrintingPreferences(bool enableAutomaticPrinting, PrintPaperProfile printPaperProfile, int printOrdersPerPage)
    {
        EnableAutomaticPrinting = enableAutomaticPrinting;
        PrintPaperProfile = printPaperProfile;
        PrintOrdersPerPage = NormalizePrintOrdersPerPage(printPaperProfile, printOrdersPerPage);
        Touch();
    }

    public void RotatePrintAgentKey(string keyHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHash);

        PrintAgentKeyHash = keyHash.Trim();
        PrintAgentName = null;
        PrintAgentPrinterName = null;
        PrintAgentLastSeenAtUtc = null;
        Touch();
    }

    public void RegisterPrintAgentHeartbeat(string agentName, string? printerName, DateTime seenAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        PrintAgentName = agentName.Trim();
        PrintAgentPrinterName = string.IsNullOrWhiteSpace(printerName) ? null : printerName.Trim();
        PrintAgentLastSeenAtUtc = seenAtUtc;
        Touch();
    }

    public void UpdateAiAssistantSettings(
        bool enableAiAssistant,
        string? model,
        string? systemPrompt,
        string? greetingMessage,
        string? redirectMessage,
        string? fallbackMessage,
        string? orderingLink,
        string? pixReceiverName,
        string? pixKey,
        string? pixMessage,
        string? serviceDays,
        string? serviceStartTime,
        string? serviceEndTime,
        int maxOutputTokens)
    {
        if (maxOutputTokens is < 80 or > 1200)
        {
            throw new ArgumentOutOfRangeException(nameof(maxOutputTokens), "A resposta da IA precisa ficar entre 80 e 1200 tokens.");
        }

        EnableAiAssistant = enableAiAssistant;
        AiAssistantModel = NormalizeAiText(model, DefaultAiAssistantModel, 80, nameof(model));
        AiAssistantSystemPrompt = NormalizeAiText(systemPrompt, DefaultAiAssistantSystemPrompt, 4000, nameof(systemPrompt));
        AiAssistantGreetingMessage = NormalizeAiText(greetingMessage, DefaultAiAssistantGreetingMessage, 500, nameof(greetingMessage));
        AiAssistantRedirectMessage = NormalizeAiText(redirectMessage, DefaultAiAssistantRedirectMessage, 500, nameof(redirectMessage));
        AiAssistantFallbackMessage = NormalizeAiText(fallbackMessage, DefaultAiAssistantFallbackMessage, 500, nameof(fallbackMessage));
        AiAssistantOrderingLink = NormalizeOptionalOrderingLink(orderingLink);
        AiAssistantPixReceiverName = NormalizeOptionalValue(pixReceiverName, 120, nameof(pixReceiverName));
        AiAssistantPixKey = NormalizeOptionalPixKey(pixKey);
        AiAssistantPixMessage = NormalizeOptionalValue(pixMessage, 500, nameof(pixMessage));
        AiAssistantServiceDays = NormalizeOptionalAiServiceDays(serviceDays, nameof(serviceDays));
        AiAssistantServiceStartTime = NormalizeOptionalAiServiceTime(serviceStartTime, nameof(serviceStartTime));
        AiAssistantServiceEndTime = NormalizeOptionalAiServiceTime(serviceEndTime, nameof(serviceEndTime));

        if ((AiAssistantServiceStartTime is null) != (AiAssistantServiceEndTime is null))
        {
            throw new ArgumentException("Preencha os dois horarios do atendimento ou deixe os dois vazios.");
        }

        AiAssistantMaxOutputTokens = maxOutputTokens;
        Touch();
    }

    public void SetAiAssistantEnabled(bool isEnabled)
    {
        EnableAiAssistant = isEnabled;
        Touch();
    }

    public void UpdateWhatsAppIntegration(
        bool enableWhatsAppAssistant,
        string? instanceId,
        string? instanceTokenCipherText,
        string? accountSecurityTokenCipherText,
        string? webhookSecretCipherText)
    {
        var normalizedInstanceId = NormalizeOptionalValue(instanceId, 80, nameof(instanceId), normalizeCase: true);
        var normalizedInstanceToken = NormalizeOptionalValue(instanceTokenCipherText, 2000, nameof(instanceTokenCipherText));
        var normalizedAccountToken = NormalizeOptionalValue(accountSecurityTokenCipherText, 2000, nameof(accountSecurityTokenCipherText));
        var normalizedWebhookSecret = NormalizeOptionalValue(webhookSecretCipherText, 2000, nameof(webhookSecretCipherText));

        EnableWhatsAppAssistant = enableWhatsAppAssistant &&
                                  !string.IsNullOrWhiteSpace(normalizedInstanceId) &&
                                  !string.IsNullOrWhiteSpace(normalizedWebhookSecret);
        WhatsAppInstanceId = normalizedInstanceId;
        WhatsAppInstanceTokenCipherText = normalizedInstanceToken;
        WhatsAppAccountSecurityTokenCipherText = normalizedAccountToken;
        WhatsAppWebhookSecretCipherText = normalizedWebhookSecret;

        if (string.IsNullOrWhiteSpace(normalizedInstanceId) || string.IsNullOrWhiteSpace(normalizedWebhookSecret))
        {
            IsWhatsAppConnected = false;
            WhatsAppConnectedPhone = null;
            WhatsAppConnectedAtUtc = null;
            WhatsAppDisconnectedAtUtc = null;
            WhatsAppLastIncomingAtUtc = null;
            WhatsAppLastOutgoingAtUtc = null;
        }

        Touch();
    }

    public void RegisterWhatsAppConnected(string? connectedPhone, DateTime occurredAtUtc)
    {
        IsWhatsAppConnected = true;
        WhatsAppConnectedPhone = NormalizeOptionalValue(connectedPhone, 40, nameof(connectedPhone));
        WhatsAppConnectedAtUtc = occurredAtUtc;
        Touch();
    }

    public void RegisterWhatsAppDisconnected(DateTime occurredAtUtc)
    {
        IsWhatsAppConnected = false;
        WhatsAppConnectedPhone = null;
        WhatsAppDisconnectedAtUtc = occurredAtUtc;
        Touch();
    }

    public void RegisterWhatsAppInbound(DateTime occurredAtUtc)
    {
        WhatsAppLastIncomingAtUtc = occurredAtUtc;

        Touch();
    }

    public void RegisterWhatsAppOutbound(DateTime occurredAtUtc)
    {
        WhatsAppLastOutgoingAtUtc = occurredAtUtc;
        Touch();
    }

    public void UpdateDeliveryFreightSettings(
        bool enabled,
        string? originPostalCode,
        decimal pricePerKm,
        decimal baseFee,
        decimal baseDistanceKm,
        int? pickupEstimatedMinutes,
        int? deliveryEstimatedMinutes)
    {
        if (pricePerKm < 0)
        {
            throw new ArgumentException("O valor por KM nao pode ser negativo.", nameof(pricePerKm));
        }

        if (pricePerKm > 1000)
        {
            throw new ArgumentException("O valor por KM precisa ser menor que R$ 1.000,00.", nameof(pricePerKm));
        }

        if (baseFee < 0)
        {
            throw new ArgumentException("A taxa minima nao pode ser negativa.", nameof(baseFee));
        }

        if (baseFee > 1000)
        {
            throw new ArgumentException("A taxa minima precisa ser menor que R$ 1.000,00.", nameof(baseFee));
        }

        if (baseDistanceKm < 0)
        {
            throw new ArgumentException("Os KM inclusos na taxa minima nao podem ser negativos.", nameof(baseDistanceKm));
        }

        if (baseDistanceKm > 200)
        {
            throw new ArgumentException("Os KM inclusos na taxa minima precisam ser menores que 200 km.", nameof(baseDistanceKm));
        }

        var normalizedPostalCode = NormalizeOptionalPostalCode(originPostalCode);

        if (enabled)
        {
            if (string.IsNullOrWhiteSpace(normalizedPostalCode))
            {
                throw new ArgumentException("Informe o CEP fixo da unidade para ativar o frete automatico.", nameof(originPostalCode));
            }

            if (pricePerKm <= 0)
            {
                throw new ArgumentException("Informe um valor por KM maior que zero para ativar o frete automatico.", nameof(pricePerKm));
            }
        }

        EnableDeliveryFreight = enabled;
        DeliveryOriginPostalCode = normalizedPostalCode;
        DeliveryFreightPricePerKm = decimal.Round(pricePerKm, 2);
        DeliveryFreightBaseFee = decimal.Round(baseFee, 2);
        DeliveryFreightBaseDistanceKm = decimal.Round(baseDistanceKm, 2);
        PickupEstimatedMinutes = NormalizeOptionalEstimatedMinutes(pickupEstimatedMinutes, nameof(pickupEstimatedMinutes));
        DeliveryEstimatedMinutes = NormalizeOptionalEstimatedMinutes(deliveryEstimatedMinutes, nameof(deliveryEstimatedMinutes));
        Touch();
    }

    public void UpdateAdminMasterPassword(string passwordHash, string cipherText, DateTime rotatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);

        AdminMasterPasswordHash = passwordHash.Trim();
        AdminMasterPasswordCipherText = cipherText.Trim();
        AdminMasterPasswordRotatedAtUtc = rotatedAtUtc;
        Touch();
    }

    public void ConnectMercadoPago(
        string userId,
        string accessTokenCipherText,
        string? refreshTokenCipherText,
        string? publicKey,
        bool liveMode,
        DateTime? tokenExpiresAtUtc,
        DateTime connectedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessTokenCipherText);

        MercadoPagoUserId = userId.Trim();
        MercadoPagoAccessTokenCipherText = accessTokenCipherText;
        MercadoPagoRefreshTokenCipherText = string.IsNullOrWhiteSpace(refreshTokenCipherText) ? null : refreshTokenCipherText;
        MercadoPagoPublicKey = NormalizeOptionalValue(publicKey, 200, nameof(publicKey));
        MercadoPagoLiveMode = liveMode;
        MercadoPagoTokenExpiresAtUtc = tokenExpiresAtUtc;
        IsMercadoPagoConnected = true;
        MercadoPagoConnectedAtUtc = connectedAtUtc;
        MercadoPagoDisconnectedAtUtc = null;
        Touch();
    }

    public void UpdateMercadoPagoTokens(
        string accessTokenCipherText,
        string? refreshTokenCipherText,
        DateTime? tokenExpiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessTokenCipherText);

        MercadoPagoAccessTokenCipherText = accessTokenCipherText;
        if (!string.IsNullOrWhiteSpace(refreshTokenCipherText))
        {
            MercadoPagoRefreshTokenCipherText = refreshTokenCipherText;
        }

        MercadoPagoTokenExpiresAtUtc = tokenExpiresAtUtc;
        Touch();
    }

    public void DisconnectMercadoPago(DateTime occurredAtUtc)
    {
        MercadoPagoUserId = null;
        MercadoPagoAccessTokenCipherText = null;
        MercadoPagoRefreshTokenCipherText = null;
        MercadoPagoPublicKey = null;
        MercadoPagoLiveMode = false;
        MercadoPagoTokenExpiresAtUtc = null;
        IsMercadoPagoConnected = false;
        MercadoPagoConnectedAtUtc = null;
        MercadoPagoDisconnectedAtUtc = occurredAtUtc;
        Touch();
    }

    private static int NormalizePrintOrdersPerPage(PrintPaperProfile printPaperProfile, int printOrdersPerPage)
    {
        if (printPaperProfile == PrintPaperProfile.Thermal80mm)
        {
            return 1;
        }

        return printOrdersPerPage is 2 or 4 ? printOrdersPerPage : 1;
    }

    private static string NormalizeAiText(string? value, string fallbackValue, int maxLength, string fieldName)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? fallbackValue
            : value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"O campo {fieldName} precisa ter no maximo {maxLength} caracteres.", fieldName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalOrderingLink(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("Use um link completo com http ou https para o pedido oficial.", nameof(value));
        }

        if (normalized.Length > 500)
        {
            throw new ArgumentException("O link oficial do pedido precisa ter no maximo 500 caracteres.", nameof(value));
        }

        return normalized;
    }

    private static string? NormalizeOptionalPixKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > 180)
        {
            throw new ArgumentException("A chave Pix precisa ter no maximo 180 caracteres.", nameof(value));
        }

        return normalized;
    }

    private static string? NormalizeOptionalValue(string? value, int maxLength, string fieldName, bool normalizeCase = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = normalizeCase
            ? value.Trim().ToUpperInvariant()
            : value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"O campo {fieldName} precisa ter no maximo {maxLength} caracteres.", fieldName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalAiServiceTime(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > 5)
        {
            throw new ArgumentException($"O campo {fieldName} precisa seguir o horario no formato HH:mm.", fieldName);
        }

        if (!TimeOnly.TryParseExact(normalized, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            throw new ArgumentException($"Use o horario no formato HH:mm para o campo {fieldName}.", fieldName);
        }

        return time.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private static string? NormalizeOptionalAiServiceDays(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var days = new SortedSet<int>();
        foreach (var rawDay in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(rawDay, NumberStyles.None, CultureInfo.InvariantCulture, out var day) || day is < 0 or > 6)
            {
                throw new ArgumentException("Os dias de atendimento precisam ficar entre 0 e 6.", fieldName);
            }

            days.Add(day);
        }

        if (days.Count == 0 || days.Count == 7)
        {
            return null;
        }

        return string.Join(',', days);
    }

    private static string? NormalizeOptionalPostalCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
        {
            throw new ArgumentException("Use um CEP valido com 8 digitos.", nameof(value));
        }

        return digits;
    }

    private static int? NormalizeOptionalEstimatedMinutes(int? value, string fieldName)
    {
        if (value is null or 0)
        {
            return null;
        }

        if (value is < 1 or > 300)
        {
            throw new ArgumentOutOfRangeException(fieldName, "O tempo estimado precisa ficar entre 1 e 300 minutos.");
        }

        return value.Value;
    }
}
