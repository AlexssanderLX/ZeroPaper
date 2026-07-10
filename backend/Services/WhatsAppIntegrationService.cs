using System.Net;
using System.Net.Http.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

public partial class WhatsAppIntegrationService : IWhatsAppIntegrationService
{
    private const string WhatsAppInstanceTokenProtectorPurpose = "ZeroPaper.WhatsApp.InstanceToken.v1";
    private const string WhatsAppAccountTokenProtectorPurpose = "ZeroPaper.WhatsApp.AccountToken.v1";
    private const string WhatsAppWebhookSecretProtectorPurpose = "ZeroPaper.WhatsApp.WebhookSecret.v1";
    private static readonly TimeSpan MaximumInboundMessageAge = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MaximumInboundMessageFutureSkew = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan RecentOutboundEchoWindow = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan RecentInboundReplayWindow = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AutomatedReplyBurstWindow = TimeSpan.FromMinutes(1);
    private const int MaximumAutomatedRepliesPerBurstWindow = 5;

    private static readonly string[] EvolutionWebhookEvents =
    [
        "MESSAGES_UPSERT",
        "CONNECTION_UPDATE",
        "QRCODE_UPDATED",
        "SEND_MESSAGE"
    ];

    private static readonly ConcurrentDictionary<string, EvolutionConnectionSnapshot> PendingEvolutionSnapshots = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> InboundCompanyLocks = new();

    private readonly HttpClient _httpClient;
    private readonly ZeroPaperDbContext _context;
    private readonly IAiAssistantService _aiAssistantService;
    private readonly IDeliveryCustomerLinkService _deliveryCustomerLinkService;
    private readonly ILogger<WhatsAppIntegrationService> _logger;
    private readonly PublicAppOptions _publicAppOptions;
    private readonly EvolutionApiOptions _evolutionOptions;
    private readonly IDataProtector _instanceTokenProtector;
    private readonly IDataProtector _accountTokenProtector;
    private readonly IDataProtector _webhookSecretProtector;

    public WhatsAppIntegrationService(
        HttpClient httpClient,
        ZeroPaperDbContext context,
        IAiAssistantService aiAssistantService,
        IDeliveryCustomerLinkService deliveryCustomerLinkService,
        IOptions<PublicAppOptions> publicAppOptions,
        IOptions<EvolutionApiOptions> evolutionOptions,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<WhatsAppIntegrationService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _aiAssistantService = aiAssistantService;
        _deliveryCustomerLinkService = deliveryCustomerLinkService;
        _publicAppOptions = publicAppOptions.Value;
        _evolutionOptions = evolutionOptions.Value;
        _logger = logger;
        _instanceTokenProtector = dataProtectionProvider.CreateProtector(WhatsAppInstanceTokenProtectorPurpose);
        _accountTokenProtector = dataProtectionProvider.CreateProtector(WhatsAppAccountTokenProtectorPurpose);
        _webhookSecretProtector = dataProtectionProvider.CreateProtector(WhatsAppWebhookSecretProtectorPurpose);
    }

    public async Task HandleReceiveWebhookAsync(string instanceId, string? key, JsonDocument payload, CancellationToken cancellationToken = default)
    {
        var company = await ResolveCompanyAsync(instanceId, key, tracking: true, cancellationToken);
        var parsedMessage = ParseZApiInboundMessage(payload.RootElement);
        await ProcessInboundMessageAsync(company, parsedMessage, cancellationToken);
    }

    public async Task HandleMessageStatusWebhookAsync(string instanceId, string? key, JsonDocument payload, CancellationToken cancellationToken = default)
    {
        var company = await ResolveCompanyAsync(instanceId, key, tracking: false, cancellationToken);
        var status = ExtractZApiMessageStatus(payload.RootElement);

        if (status.Ids.Count == 0 || string.IsNullOrWhiteSpace(status.Status))
        {
            return;
        }

        var occurredAtUtc = status.MomentUtc ?? DateTime.UtcNow;
        var messages = await _context.WhatsAppMessages
            .Where(item => item.CompanyId == company.Id && item.ExternalMessageId != null && status.Ids.Contains(item.ExternalMessageId))
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.UpdateStatus(status.Status!, occurredAtUtc);
        }

        if (messages.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task HandleConnectedWebhookAsync(string instanceId, string? key, JsonDocument payload, CancellationToken cancellationToken = default)
    {
        var company = await ResolveCompanyAsync(instanceId, key, tracking: true, cancellationToken);
        company.RegisterWhatsAppConnected(ExtractZApiConnectionPhone(payload.RootElement), DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleDisconnectedWebhookAsync(string instanceId, string? key, JsonDocument payload, CancellationToken cancellationToken = default)
    {
        var company = await ResolveCompanyAsync(instanceId, key, tracking: true, cancellationToken);
        company.RegisterWhatsAppDisconnected(DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleEvolutionWebhookAsync(string instanceName, string? key, JsonDocument payload, CancellationToken cancellationToken = default)
    {
        var envelope = ParseEvolutionEnvelope(payload.RootElement);
        _logger.LogDebug(
            "Webhook Evolution recebido para instancia {Instance}. Evento: {Event}. Data detectada: {HasData}.",
            instanceName,
            string.IsNullOrWhiteSpace(envelope.Event) ? "(vazio)" : envelope.Event,
            envelope.Data.ValueKind != JsonValueKind.Undefined);

        var company = await ResolveCompanyAsync(instanceName, key, tracking: true, cancellationToken);
        var normalizedEvent = NormalizeEvolutionEventName(envelope.Event);

        switch (normalizedEvent)
        {
            case "messages.upsert":
                await ProcessInboundMessageAsync(company, ParseEvolutionInboundMessage(envelope.Data), cancellationToken);
                break;
            case "connection.update":
                await HandleEvolutionConnectionUpdateAsync(company, envelope.Data, cancellationToken);
                break;
            case "qrcode.updated":
                HandleEvolutionQrCodeUpdated(company, envelope.Data);
                break;
            case "send.message":
                await HandleEvolutionSendMessageAsync(company, envelope.Data, cancellationToken);
                break;
            default:
                _logger.LogDebug(
                    "Evento Evolution ignorado para a unidade {CompanyId}. Evento bruto: {Event}. Trecho: {PayloadExcerpt}",
                    company.Id,
                    envelope.Event,
                    TruncateWebhookPayload(payload.RootElement.ToString()));
                break;
        }
    }

    public async Task<WhatsAppConnectionSnapshotDto> PrepareEvolutionConnectionAsync(
        Guid companyId,
        string? phoneNumber = null,
        bool forceNewSession = false,
        CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(item => item.Id == companyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");

        if (!IsEvolutionConfigured())
        {
            throw new InvalidOperationException("A Evolution Lite ainda nao esta configurada no servidor do ZeroPaper.");
        }

        var instanceName = string.IsNullOrWhiteSpace(company.WhatsAppInstanceId)
            ? null
            : NormalizeEvolutionInstanceName(company.WhatsAppInstanceId);
        var webhookSecretCipher = company.WhatsAppWebhookSecretCipherText;
        var changed = false;

        if (string.IsNullOrWhiteSpace(instanceName))
        {
            instanceName = BuildEvolutionInstanceName(company);
            changed = true;
        }
        else if (!string.Equals(instanceName, company.WhatsAppInstanceId, StringComparison.Ordinal))
        {
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(webhookSecretCipher))
        {
            webhookSecretCipher = _webhookSecretProtector.Protect(CreateWebhookSecret());
            changed = true;
        }

        if (changed)
        {
            company.UpdateWhatsAppIntegration(
                company.EnableWhatsAppAssistant,
                instanceName,
                company.WhatsAppInstanceTokenCipherText,
                company.WhatsAppAccountSecurityTokenCipherText,
                webhookSecretCipher);

            await _context.SaveChangesAsync(cancellationToken);
        }

        var requestedPhone = NormalizeConnectionPhone(phoneNumber);
        var connectionSnapshot = await GetEvolutionConnectionStateAsync(company.WhatsAppInstanceId!, cancellationToken);
        if (!connectionSnapshot.InstanceExists)
        {
            connectionSnapshot = await CreateEvolutionInstanceAsync(company, cancellationToken);
        }

        await EnsureEvolutionWebhookAsync(company, cancellationToken);

        if (forceNewSession && connectionSnapshot.InstanceExists)
        {
            await LogoutEvolutionInstanceAsync(company.WhatsAppInstanceId!, cancellationToken);
            company.RegisterWhatsAppDisconnected(DateTime.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);

            connectionSnapshot = new EvolutionConnectionSnapshot
            {
                State = "close",
                IsConnected = false
            }.WithExists();
        }

        if (!connectionSnapshot.IsConnected)
        {
            var connectSnapshot = await ConnectEvolutionInstanceAsync(company.WhatsAppInstanceId!, requestedPhone, cancellationToken);
            connectionSnapshot = connectionSnapshot.Merge(connectSnapshot);
        }

        if (!connectionSnapshot.IsConnected && !connectionSnapshot.HasQrPayload)
        {
            connectionSnapshot = await WaitForEvolutionQrCodeAsync(company.WhatsAppInstanceId!, requestedPhone, connectionSnapshot, cancellationToken);
        }

        if (connectionSnapshot.IsConnected && string.IsNullOrWhiteSpace(connectionSnapshot.ConnectedPhone))
        {
            connectionSnapshot.ConnectedPhone = await GetEvolutionInstanceConnectedPhoneAsync(company.WhatsAppInstanceId!, cancellationToken);
        }

        if (connectionSnapshot.IsConnected)
        {
            company.RegisterWhatsAppConnected(connectionSnapshot.ConnectedPhone, DateTime.UtcNow);
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "SecurityAudit action={Action} company={CompanyId} forcedNewSession={ForcedNewSession} connected={Connected}",
            "prepare-whatsapp-connection",
            company.Id,
            forceNewSession,
            connectionSnapshot.IsConnected);

        return new WhatsAppConnectionSnapshotDto
        {
            ServerConfigured = true,
            InstanceConfigured = !string.IsNullOrWhiteSpace(company.WhatsAppInstanceId),
            InstanceName = company.WhatsAppInstanceId ?? string.Empty,
            State = connectionSnapshot.State,
            IsConnected = connectionSnapshot.IsConnected,
            ConnectedPhone = connectionSnapshot.ConnectedPhone ?? company.WhatsAppConnectedPhone,
            QrCodeBase64 = connectionSnapshot.QrCodeBase64,
            QrCodeText = connectionSnapshot.QrCodeText,
            PairingCode = connectionSnapshot.PairingCode,
            Message = connectionSnapshot.IsConnected
                ? "Numero conectado. O ZeroPaper ja pode responder pelo WhatsApp."
                : connectionSnapshot.HasQrPayload || !string.IsNullOrWhiteSpace(connectionSnapshot.PairingCode)
                    ? string.IsNullOrWhiteSpace(requestedPhone)
                        ? "Leia o QR Code com o WhatsApp deste numero para concluir a conexao."
                        : "Conexao preparada para o numero informado. Use o QR Code ou o codigo de pareamento para concluir."
                    : "A instancia foi preparada, mas a Evolution ainda nao devolveu QR Code nem codigo de pareamento nesta VPS."
        };
    }

    public async Task TrySendDeliveryOrderConfirmationAsync(Guid orderId, bool isUpdate, CancellationToken cancellationToken = default)
    {
        var order = await _context.CustomerOrders
            .Include(item => item.Company)
            .Include(item => item.DiningTable)
                .ThenInclude(item => item.QrCodeAccess)
            .FirstOrDefaultAsync(
                item => item.Id == orderId &&
                        item.IsActive &&
                        item.DiningTable.IsDeliveryChannel,
                cancellationToken);

        if (order is null)
        {
            return;
        }

        var company = order.Company;
        if (!company.EnableWhatsAppAssistant || string.IsNullOrWhiteSpace(order.DeliveryPhone))
        {
            return;
        }

        var normalizedPhone = NormalizePhoneForWhatsApp(order.DeliveryPhone);
        var message = await BuildDeliveryOrderMessageAsync(order, isUpdate, cancellationToken);
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var conversation = await GetOrCreateConversationAsync(company, normalizedPhone, order.CustomerName, cancellationToken);
        var sendResult = await SendTextMessageAsync(company, normalizedPhone, message, cancellationToken);

        conversation.RegisterOutbound(message, DateTime.UtcNow);
        company.RegisterWhatsAppOutbound(DateTime.UtcNow);

        var outboundMessage = new WhatsAppMessage(
            company.TenantId,
            company.Id,
            conversation.Id,
            isInbound: false,
            messageType: isUpdate ? "order-update" : "order-confirmation",
            content: message,
            externalMessageId: sendResult.ExternalMessageId,
            generatedByAi: false);

        outboundMessage.UpdateStatus(sendResult.Status, DateTime.UtcNow);
        if (!sendResult.Succeeded)
        {
            outboundMessage.MarkFailed();
        }

        await _context.WhatsAppMessages.AddAsync(outboundMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessInboundMessageAsync(Company company, ParsedInboundMessage parsedMessage, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        if (parsedMessage.IsIgnored ||
            !company.EnableWhatsAppAssistant ||
            parsedMessage.OccurredAtUtc < nowUtc.Subtract(MaximumInboundMessageAge) ||
            parsedMessage.OccurredAtUtc > nowUtc.Add(MaximumInboundMessageFutureSkew))
        {
            return;
        }

        var normalizedPhone = parsedMessage.IsEvolutionMessage
            ? NormalizeEvolutionChatIdentifier(parsedMessage.Phone)
            : NormalizePhoneForWhatsApp(parsedMessage.Phone);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return;
        }

        var inboundLock = InboundCompanyLocks.GetOrAdd(company.Id, _ => new SemaphoreSlim(1, 1));
        await inboundLock.WaitAsync(cancellationToken);

        try
        {
            await ProcessInboundMessageWithinLockAsync(company, parsedMessage, normalizedPhone, cancellationToken);
        }
        finally
        {
            inboundLock.Release();
        }
    }

    private async Task ProcessInboundMessageWithinLockAsync(
        Company company,
        ParsedInboundMessage parsedMessage,
        string normalizedPhone,
        CancellationToken cancellationToken)
    {
        if (!company.EnableWhatsAppAssistant)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(parsedMessage.ExternalMessageId))
        {
            var alreadyProcessed = await _context.WhatsAppMessages
                .AsNoTracking()
                .AnyAsync(
                    item => item.CompanyId == company.Id &&
                            item.ExternalMessageId == parsedMessage.ExternalMessageId,
                    cancellationToken);

            if (alreadyProcessed)
            {
                return;
            }
        }

        var conversation = await GetOrCreateConversationAsync(company, normalizedPhone, parsedMessage.CustomerName, cancellationToken);
        var history = await BuildConversationHistoryAsync(conversation.Id, cancellationToken);
        var deliveryContext = await BuildDeliveryCustomerContextAsync(company, normalizedPhone, cancellationToken);
        var personalLinkAlreadySentRecently = HasPersonalDeliveryLinkInHistory(history, deliveryContext.CustomerLink);
        var shouldAllowLinkResend = HasExplicitLinkRequest(parsedMessage.Message);
        var isOrderTrackingRequest = HasOrderTrackingIntent(parsedMessage.Message);
        var isSubmittedOrderFollowUp = HasSubmittedOrderFollowUpIntent(parsedMessage.Message);
        var customerPromptContext = BuildCustomerPromptContext(deliveryContext, personalLinkAlreadySentRecently, shouldAllowLinkResend);
        var nowUtc = DateTime.UtcNow;

        if (await IsRecentOutboundEchoAsync(conversation.Id, parsedMessage.Message, nowUtc, cancellationToken))
        {
            _logger.LogWarning(
                "Eco de resposta automatica ignorado para a unidade {CompanyId}: conversa {ConversationId}.",
                company.Id,
                conversation.Id);
            return;
        }

        if (await IsRecentInboundReplayAsync(conversation.Id, parsedMessage.Message, nowUtc, cancellationToken))
        {
            _logger.LogWarning(
                "Replay de mensagem recebida ignorado para a unidade {CompanyId}: conversa {ConversationId}.",
                company.Id,
                conversation.Id);
            return;
        }

        conversation.RegisterInbound(parsedMessage.CustomerName, parsedMessage.Message, nowUtc);
        company.RegisterWhatsAppInbound(nowUtc);

        var inboundMessage = new WhatsAppMessage(
            company.TenantId,
            company.Id,
            conversation.Id,
            isInbound: true,
            messageType: parsedMessage.MessageType,
            content: parsedMessage.Message,
            externalMessageId: parsedMessage.ExternalMessageId);

        await _context.WhatsAppMessages.AddAsync(inboundMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        if (await HasAutomatedReplyBurstAsync(company.Id, nowUtc, cancellationToken))
        {
            company.SetAiAssistantEnabled(false);
            company.UpdateWhatsAppIntegration(
                enableWhatsAppAssistant: false,
                instanceId: company.WhatsAppInstanceId,
                instanceTokenCipherText: company.WhatsAppInstanceTokenCipherText,
                accountSecurityTokenCipherText: company.WhatsAppAccountSecurityTokenCipherText,
                webhookSecretCipherText: company.WhatsAppWebhookSecretCipherText);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogCritical(
                "Fusivel anti-spam acionado para a unidade {CompanyId}: limite de {MaximumReplies} respostas automaticas em {WindowSeconds} segundos atingido. O atendimento automatico por WhatsApp foi desativado.",
                company.Id,
                MaximumAutomatedRepliesPerBurstWindow,
                AutomatedReplyBurstWindow.TotalSeconds);
            return;
        }

        string replyText;
        bool generatedByAi;
        string replyMessageType;
        if (!company.EnableAiAssistant)
        {
            return;
        }
        else if (isOrderTrackingRequest && !deliveryContext.HasKnownOrder)
        {
            replyText = "Nao encontrei nenhum pedido para este numero.";
            generatedByAi = false;
            replyMessageType = "fallback";
        }
        else if (isOrderTrackingRequest)
        {
            replyText = BuildOrderTrackingReply(deliveryContext);
            generatedByAi = false;
            replyMessageType = "fallback";
        }
        else if (isSubmittedOrderFollowUp)
        {
            replyText = BuildSubmittedOrderFollowUpReply(company, deliveryContext);
            generatedByAi = false;
            replyMessageType = "fallback";
        }
        else
        {
            try
            {
                var aiResponse = await _aiAssistantService.GenerateReplyAsync(
                    company.Id,
                    "WhatsApp",
                    parsedMessage.Message,
                    history,
                    customerPromptContext,
                    cancellationToken);

                replyText = NormalizeMessageForWhatsApp(aiResponse.Reply);
                generatedByAi = true;
                replyMessageType = "ai-reply";
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                _logger.LogWarning(exception, "Falha ao gerar resposta da IA para a unidade {CompanyId}.", company.Id);
                replyText = BuildFallbackReply(company);
                generatedByAi = false;
                replyMessageType = "fallback";
            }
        }

        if (!isOrderTrackingRequest && !isSubmittedOrderFollowUp)
        {
            replyText = EnsurePersonalDeliveryLinkWhenNeeded(
                replyText,
                parsedMessage.Message,
                deliveryContext,
                personalLinkAlreadySentRecently,
                shouldAllowLinkResend);
        }
        replyText = ApplyDeliveryConversationSafetyRules(replyText, parsedMessage.Message, company, deliveryContext);

        if (string.IsNullOrWhiteSpace(replyText))
        {
            return;
        }

        var sendResult = await SendTextMessageAsync(company, normalizedPhone, replyText, cancellationToken);

        conversation.RegisterOutbound(replyText, DateTime.UtcNow);
        company.RegisterWhatsAppOutbound(DateTime.UtcNow);

        var outboundMessage = new WhatsAppMessage(
            company.TenantId,
            company.Id,
            conversation.Id,
            isInbound: false,
            messageType: replyMessageType,
            content: replyText,
            externalMessageId: sendResult.ExternalMessageId,
            generatedByAi: generatedByAi);

        outboundMessage.UpdateStatus(sendResult.Status, DateTime.UtcNow);
        if (!sendResult.Succeeded)
        {
            outboundMessage.MarkFailed();
        }

        await _context.WhatsAppMessages.AddAsync(outboundMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsRecentOutboundEchoAsync(
        Guid conversationId,
        string content,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var minimumCreatedAtUtc = nowUtc.Subtract(RecentOutboundEchoWindow);
        return await _context.WhatsAppMessages
            .AsNoTracking()
            .AnyAsync(
                item => item.WhatsAppConversationId == conversationId &&
                        !item.IsInbound &&
                        item.CreatedAtUtc >= minimumCreatedAtUtc &&
                        item.Content == content,
                cancellationToken);
    }

    private async Task<bool> IsRecentInboundReplayAsync(
        Guid conversationId,
        string content,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var minimumCreatedAtUtc = nowUtc.Subtract(RecentInboundReplayWindow);
        return await _context.WhatsAppMessages
            .AsNoTracking()
            .AnyAsync(
                item => item.WhatsAppConversationId == conversationId &&
                        item.IsInbound &&
                        item.CreatedAtUtc >= minimumCreatedAtUtc &&
                        item.Content == content,
                cancellationToken);
    }

    private async Task<bool> HasAutomatedReplyBurstAsync(
        Guid companyId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var minimumCreatedAtUtc = nowUtc.Subtract(AutomatedReplyBurstWindow);
        var recentAutomatedReplyCount = await _context.WhatsAppMessages
            .AsNoTracking()
            .CountAsync(
                item => item.CompanyId == companyId &&
                        !item.IsInbound &&
                        item.CreatedAtUtc >= minimumCreatedAtUtc &&
                        (item.GeneratedByAi ||
                         item.MessageType == "fallback"),
                cancellationToken);

        return recentAutomatedReplyCount >= MaximumAutomatedRepliesPerBurstWindow;
    }

    private async Task HandleEvolutionConnectionUpdateAsync(Company company, JsonElement data, CancellationToken cancellationToken)
    {
        var snapshot = ParseEvolutionQrCodeSnapshot(data);
        _logger.LogInformation(
            "Connection update Evolution para a unidade {CompanyId}. Estado: {State}. Base64: {HasBase64}. Codigo bruto: {HasCode}. Pairing: {HasPairingCode}.",
            company.Id,
            snapshot.State,
            !string.IsNullOrWhiteSpace(snapshot.QrCodeBase64),
            !string.IsNullOrWhiteSpace(snapshot.QrCodeText),
            !string.IsNullOrWhiteSpace(snapshot.PairingCode));
        var state = snapshot.State;
        if (string.Equals(state, "open", StringComparison.OrdinalIgnoreCase))
        {
            PendingEvolutionSnapshots.TryRemove(company.WhatsAppInstanceId ?? string.Empty, out _);
            company.RegisterWhatsAppConnected(snapshot.ConnectedPhone ?? ExtractEvolutionConnectionPhone(data), DateTime.UtcNow);
        }
        else
        {
            StoreEvolutionSnapshot(company.WhatsAppInstanceId, snapshot);
            company.RegisterWhatsAppDisconnected(DateTime.UtcNow);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private void HandleEvolutionQrCodeUpdated(Company company, JsonElement data)
    {
        var snapshot = ParseEvolutionQrCodeSnapshot(data);
        StoreEvolutionSnapshot(company.WhatsAppInstanceId, snapshot);

        _logger.LogInformation(
            "QR do WhatsApp atualizado para a unidade {CompanyId}. Base64: {HasBase64}. Codigo bruto: {HasCode}. Pairing: {HasPairingCode}.",
            company.Id,
            !string.IsNullOrWhiteSpace(snapshot.QrCodeBase64),
            !string.IsNullOrWhiteSpace(snapshot.QrCodeText),
            !string.IsNullOrWhiteSpace(snapshot.PairingCode));
    }

    private async Task HandleEvolutionSendMessageAsync(Company company, JsonElement data, CancellationToken cancellationToken)
    {
        var externalMessageId = GetString(data, ["key", "id"]);
        if (string.IsNullOrWhiteSpace(externalMessageId))
        {
            return;
        }

        var message = await _context.WhatsAppMessages
            .FirstOrDefaultAsync(item => item.CompanyId == company.Id && item.ExternalMessageId == externalMessageId, cancellationToken);

        if (message is null)
        {
            return;
        }

        message.UpdateStatus("SENT", DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Company> ResolveCompanyAsync(string instanceId, string? key, bool tracking, CancellationToken cancellationToken)
    {
        var normalizedInstanceId = NormalizeEvolutionInstanceName(instanceId);
        var query = tracking ? _context.Companies.AsQueryable() : _context.Companies.AsNoTracking();

        var company = await query
            .FirstOrDefaultAsync(
                item => item.IsActive &&
                        item.WhatsAppInstanceId == normalizedInstanceId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Integracao de WhatsApp nao encontrada.");

        var storedSecret = UnprotectOrNull(_webhookSecretProtector, company.WhatsAppWebhookSecretCipherText);
        if (!SecretsMatch(storedSecret, key))
        {
            throw new KeyNotFoundException("Integracao de WhatsApp nao encontrada.");
        }

        return company;
    }

    private async Task<WhatsAppConversation> GetOrCreateConversationAsync(
        Company company,
        string normalizedPhone,
        string? customerName,
        CancellationToken cancellationToken)
    {
        var conversation = await _context.WhatsAppConversations
            .FirstOrDefaultAsync(
                item => item.CompanyId == company.Id &&
                        item.ExternalPhone == normalizedPhone,
                cancellationToken);

        if (conversation is not null)
        {
            conversation.Activate();
            return conversation;
        }

        conversation = new WhatsAppConversation(company.TenantId, company.Id, normalizedPhone, customerName);
        await _context.WhatsAppConversations.AddAsync(conversation, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return conversation;
        }
        catch (DbUpdateException exception) when (IsDuplicateWhatsAppConversationException(exception))
        {
            _context.Entry(conversation).State = EntityState.Detached;

            var existingConversation = await _context.WhatsAppConversations
                .FirstAsync(
                    item => item.CompanyId == company.Id &&
                            item.ExternalPhone == normalizedPhone,
                    cancellationToken);

            existingConversation.Activate();
            return existingConversation;
        }
    }

    private async Task<List<AiAssistantConversationTurnDto>> BuildConversationHistoryAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return await _context.WhatsAppMessages
            .AsNoTracking()
            .Where(item => item.WhatsAppConversationId == conversationId && item.IsActive)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(12)
            .OrderBy(item => item.CreatedAtUtc)
            .Select(item => new AiAssistantConversationTurnDto
            {
                Role = item.IsInbound ? "user" : "assistant",
                Content = item.Content
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<DeliveryCustomerContext> BuildDeliveryCustomerContextAsync(
        Company company,
        string normalizedPhone,
        CancellationToken cancellationToken)
    {
        var deliveryTable = await _context.DiningTables
            .AsNoTracking()
            .Include(item => item.QrCodeAccess)
            .FirstOrDefaultAsync(
                item => item.CompanyId == company.Id &&
                        item.IsActive &&
                        item.IsDeliveryChannel &&
                        item.QrCodeAccess != null &&
                        item.QrCodeAccess.IsActive,
                cancellationToken);

        if (deliveryTable?.QrCodeAccess is null)
        {
            return DeliveryCustomerContext.Empty;
        }

        var activeOrder = await FindActiveDeliveryOrderForPhoneAsync(company.Id, normalizedPhone, cancellationToken);
        var profile = await FindDeliveryCustomerProfileForPhoneAsync(company.Id, normalizedPhone, cancellationToken);
        var lastOrder = profile is null
            ? await FindLastDeliveryOrderForPhoneAsync(company.Id, normalizedPhone, cancellationToken)
            : null;

        if (profile is null && lastOrder is not null)
        {
            profile = await EnsureDeliveryCustomerProfileFromOrderAsync(lastOrder, normalizedPhone, cancellationToken);
        }

        var customerLink = profile is not null
            ? await BuildPersonalDeliveryUrlAsync(company.Id, normalizedPhone, cancellationToken)
            : BuildGenericDeliveryUrl(deliveryTable.QrCodeAccess.PublicCode);

        if (string.IsNullOrWhiteSpace(customerLink))
        {
            return DeliveryCustomerContext.Empty;
        }

        var contextLines = new List<string>
        {
            $"Link curto para este atendimento: {customerLink}",
            "Se o cliente demonstrar que quer pedir, ver cardapio, fazer delivery ou comprar, envie este link literal nessa resposta.",
            "Nao use frases como \"pelo link que te mandei\" sem reenviar o link.",
            "Explique como o fluxo funciona, mas nao responda cardapio detalhado, itens, precos ou disponibilidade pelo WhatsApp.",
            BuildEstimatedWaitContext(company),
            BuildPixPromptContext(company),
            "Toda conferencia de Pix e manual pela equipe. Nao valide comprovante, nao solicite frase de confirmacao e nao registre confirmacao automaticamente.",
            "Nao informe endereco completo na conversa."
        };

        if (profile is not null)
        {
            if (!string.IsNullOrWhiteSpace(profile.CustomerName))
            {
                contextLines.Add($"Nome reconhecido no cadastro de delivery: {profile.CustomerName.Trim()}.");
            }

            contextLines.Add($"Ultimo delivery salvo deste telefone: {profile.LastOrderAtUtc.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR"))}.");
            if (activeOrder is not null)
            {
                contextLines.Add($"Pedido ativo deste telefone: #{activeOrder.Number}, status {activeOrder.Status}.");
            }
            contextLines.Add("O link ja abre o site com os dados salvos deste numero. Oriente o cliente a conferir ou alterar se algo mudou.");
            var trackingLink = await BuildPersonalDeliveryTrackingUrlAsync(company.Id, normalizedPhone, cancellationToken);
            return new DeliveryCustomerContext(
                string.Join("\n", contextLines),
                customerLink,
                hasSavedProfile: true,
                hasKnownOrder: activeOrder is not null,
                activeOrder?.Number,
                activeOrder?.Status.ToString(),
                trackingLink);
        }

        if (lastOrder is null)
        {
            contextLines.Add("Este telefone ainda nao tem pedido anterior encontrado nesta unidade.");
            contextLines.Add("Este link abre o pedido normal, sem preencher dados pessoais.");
            return new DeliveryCustomerContext(string.Join("\n", contextLines), customerLink, hasSavedProfile: false, hasKnownOrder: false);
        }

        if (!string.IsNullOrWhiteSpace(lastOrder.CustomerName))
        {
            contextLines.Add($"Nome reconhecido no ultimo pedido: {lastOrder.CustomerName.Trim()}.");
        }

        contextLines.Add($"Ultimo pedido deste telefone: {lastOrder.SubmittedAtUtc.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR"))}.");
        if (activeOrder is not null)
        {
            contextLines.Add($"Pedido ativo deste telefone: #{activeOrder.Number}, status {activeOrder.Status}.");
        }
        contextLines.Add("O link ja abre o site com os dados salvos deste numero. Oriente o cliente a conferir ou alterar se algo mudou.");
        var lastOrderTrackingLink = await BuildPersonalDeliveryTrackingUrlAsync(company.Id, normalizedPhone, cancellationToken);
        return new DeliveryCustomerContext(
            string.Join("\n", contextLines),
            customerLink,
            hasSavedProfile: true,
            hasKnownOrder: activeOrder is not null,
            activeOrder?.Number,
            activeOrder?.Status.ToString(),
            lastOrderTrackingLink);
    }

    private async Task<DeliveryCustomerProfile?> EnsureDeliveryCustomerProfileFromOrderAsync(
        CustomerOrder lastOrder,
        string normalizedPhone,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return null;
        }

        var existingProfile = await _context.DeliveryCustomerProfiles
            .FirstOrDefaultAsync(
                item =>
                    item.CompanyId == lastOrder.CompanyId &&
                    item.Phone == normalizedPhone,
                cancellationToken);

        if (existingProfile is not null)
        {
            existingProfile.Activate();
            existingProfile.Update(
                lastOrder.CustomerName,
                lastOrder.DeliveryAddress,
                lastOrder.DeliveryNumber,
                null,
                lastOrder.DeliveryComplement,
                lastOrder.DeliveryPostalCode,
                lastOrder.SubmittedAtUtc);
            await _context.SaveChangesAsync(cancellationToken);
            return existingProfile;
        }

        var profile = new DeliveryCustomerProfile(
            lastOrder.TenantId,
            lastOrder.CompanyId,
            normalizedPhone,
            lastOrder.CustomerName,
            lastOrder.DeliveryAddress,
            lastOrder.DeliveryNumber,
            null,
            lastOrder.DeliveryComplement,
            lastOrder.DeliveryPostalCode,
            lastOrder.SubmittedAtUtc);

        await _context.DeliveryCustomerProfiles.AddAsync(profile, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return profile;
        }
        catch (DbUpdateException)
        {
            _context.ChangeTracker.Clear();
            return await FindDeliveryCustomerProfileForPhoneAsync(lastOrder.CompanyId, normalizedPhone, cancellationToken);
        }
    }

    private async Task<DeliveryCustomerProfile?> FindDeliveryCustomerProfileForPhoneAsync(
        Guid companyId,
        string normalizedPhone,
        CancellationToken cancellationToken)
    {
        return await _context.DeliveryCustomerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.CompanyId == companyId &&
                        item.Phone == normalizedPhone &&
                        item.IsActive,
                cancellationToken);
    }

    private async Task<CustomerOrder?> FindLastDeliveryOrderForPhoneAsync(
        Guid companyId,
        string normalizedPhone,
        CancellationToken cancellationToken)
    {
        var recentOrders = await _context.CustomerOrders
            .AsNoTracking()
            .Include(item => item.DiningTable)
            .Where(item =>
                item.CompanyId == companyId &&
                item.IsActive &&
                item.DiningTable.IsDeliveryChannel &&
                item.DeliveryPhone != null)
            .OrderByDescending(item => item.SubmittedAtUtc)
            .Take(250)
            .ToListAsync(cancellationToken);

        return recentOrders.FirstOrDefault(item =>
            string.Equals(
                NormalizePhoneForWhatsApp(item.DeliveryPhone ?? string.Empty),
                normalizedPhone,
                StringComparison.Ordinal));
    }

    private async Task<CustomerOrder?> FindActiveDeliveryOrderForPhoneAsync(
        Guid companyId,
        string normalizedPhone,
        CancellationToken cancellationToken)
    {
        var recentOrders = await _context.CustomerOrders
            .AsNoTracking()
            .Include(item => item.DiningTable)
            .Where(item =>
                item.CompanyId == companyId &&
                item.IsActive &&
                item.DiningTable.IsDeliveryChannel &&
                item.DeliveryPhone != null &&
                item.Status != OrderStatus.Cancelled &&
                item.Status != OrderStatus.Delivered)
            .OrderByDescending(item => item.SubmittedAtUtc)
            .Take(250)
            .ToListAsync(cancellationToken);

        return recentOrders.FirstOrDefault(item =>
            string.Equals(
                NormalizePhoneForWhatsApp(item.DeliveryPhone ?? string.Empty),
                normalizedPhone,
                StringComparison.Ordinal));
    }

    private async Task<SendMessageResult> SendTextMessageAsync(Company company, string phone, string message, CancellationToken cancellationToken)
    {
        if (IsEvolutionConfigured())
        {
            return await SendTextViaEvolutionAsync(company, phone, message, cancellationToken);
        }

        return await SendTextViaZApiAsync(company, phone, message, cancellationToken);
    }

    private async Task<SendMessageResult> SendTextViaEvolutionAsync(Company company, string phone, string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(company.WhatsAppInstanceId))
        {
            return SendMessageResult.Failed("MISSING_INSTANCE");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildEvolutionApiUrl($"message/sendText/{Uri.EscapeDataString(company.WhatsAppInstanceId)}"));
        request.Headers.TryAddWithoutValidation("apikey", ResolveEvolutionApiKey());
        request.Content = JsonContent.Create(new
        {
            number = phone,
            text = message
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Falha ao enviar mensagem via Evolution para a unidade {CompanyId}. Status: {StatusCode}.",
                company.Id,
                (int)response.StatusCode);

            return SendMessageResult.Failed(((int)response.StatusCode).ToString());
        }

        return new SendMessageResult(
            succeeded: true,
            externalMessageId: ExtractEvolutionExternalMessageId(responseText),
            status: "SENT");
    }

    private async Task<SendMessageResult> SendTextViaZApiAsync(Company company, string phone, string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(company.WhatsAppInstanceId))
        {
            return SendMessageResult.Failed("MISSING_INSTANCE");
        }

        var instanceToken = UnprotectOrNull(_instanceTokenProtector, company.WhatsAppInstanceTokenCipherText);
        if (string.IsNullOrWhiteSpace(instanceToken))
        {
            return SendMessageResult.Failed("MISSING_TOKEN");
        }

        var accountToken = UnprotectOrNull(_accountTokenProtector, company.WhatsAppAccountSecurityTokenCipherText);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"instances/{Uri.EscapeDataString(company.WhatsAppInstanceId)}/token/{Uri.EscapeDataString(instanceToken)}/send-text");

        if (!string.IsNullOrWhiteSpace(accountToken))
        {
            request.Headers.TryAddWithoutValidation("Client-Token", accountToken);
        }

        request.Content = JsonContent.Create(new
        {
            phone,
            message
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Falha ao enviar mensagem para o WhatsApp da unidade {CompanyId}. Status: {StatusCode}.",
                company.Id,
                (int)response.StatusCode);

            return SendMessageResult.Failed(((int)response.StatusCode).ToString());
        }

        return new SendMessageResult(
            succeeded: true,
            externalMessageId: ExtractZApiExternalMessageId(responseText),
            status: "SENT");
    }

    private async Task EnsureEvolutionWebhookAsync(Company company, CancellationToken cancellationToken)
    {
        var webhookSecret = UnprotectOrNull(_webhookSecretProtector, company.WhatsAppWebhookSecretCipherText);
        if (string.IsNullOrWhiteSpace(company.WhatsAppInstanceId) || string.IsNullOrWhiteSpace(webhookSecret))
        {
            throw new ArgumentException("A unidade ainda nao tem a configuracao minima do WhatsApp.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            BuildEvolutionApiUrl($"webhook/set/{Uri.EscapeDataString(company.WhatsAppInstanceId)}"));
        request.Headers.TryAddWithoutValidation("apikey", ResolveEvolutionApiKey());
        request.Content = JsonContent.Create(new
        {
            webhook = new
            {
                enabled = true,
                url = BuildEvolutionWebhookUrl(company.WhatsAppInstanceId, webhookSecret),
                byEvents = false,
                webhookByEvents = false,
                webhook_base64 = true,
                webhookBase64 = true,
                @base64 = true,
                events = EvolutionWebhookEvents
            }
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Nao foi possivel preparar o webhook da Evolution Lite na unidade.");
        }
    }

    private async Task<EvolutionConnectionSnapshot> CreateEvolutionInstanceAsync(Company company, CancellationToken cancellationToken)
    {
        var webhookSecret = UnprotectOrNull(_webhookSecretProtector, company.WhatsAppWebhookSecretCipherText);
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildEvolutionApiUrl("instance/create"));
        request.Headers.TryAddWithoutValidation("apikey", ResolveEvolutionApiKey());
        request.Content = JsonContent.Create(new
        {
            instanceName = company.WhatsAppInstanceId,
            qrcode = true,
            integration = ResolveEvolutionIntegration(),
            webhook = new
            {
                enabled = true,
                url = BuildEvolutionWebhookUrl(company.WhatsAppInstanceId, webhookSecret),
                byEvents = false,
                webhookByEvents = false,
                webhook_base64 = true,
                webhookBase64 = true,
                @base64 = true,
                events = EvolutionWebhookEvents
            }
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Nao foi possivel criar a instancia na Evolution Lite.");
        }

        return ParseEvolutionConnectionSnapshot(responseText).WithExists();
    }

    private async Task<EvolutionConnectionSnapshot> ConnectEvolutionInstanceAsync(
        string instanceName,
        string? phoneNumber,
        CancellationToken cancellationToken)
    {
        var relativePath = string.IsNullOrWhiteSpace(phoneNumber)
            ? $"instance/connect/{Uri.EscapeDataString(instanceName)}"
            : $"instance/connect/{Uri.EscapeDataString(instanceName)}?number={Uri.EscapeDataString(phoneNumber)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildEvolutionApiUrl(relativePath));
        request.Headers.TryAddWithoutValidation("apikey", ResolveEvolutionApiKey());

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Nao foi possivel abrir a conexao da instancia na Evolution Lite.");
        }

        var snapshot = ParseEvolutionConnectionSnapshot(responseText);
        if (string.IsNullOrWhiteSpace(snapshot.State) && !snapshot.IsConnected && !snapshot.HasQrPayload)
        {
            return EvolutionConnectionSnapshot.Missing(instanceName);
        }

        return snapshot.WithExists();
    }

    private async Task LogoutEvolutionInstanceAsync(string instanceName, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, BuildEvolutionApiUrl($"instance/logout/{Uri.EscapeDataString(instanceName)}"));
        request.Headers.TryAddWithoutValidation("apikey", ResolveEvolutionApiKey());

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException("Nao foi possivel encerrar a conexao atual do WhatsApp nesta unidade.");
        }
    }

    private async Task<EvolutionConnectionSnapshot> WaitForEvolutionQrCodeAsync(
        string instanceName,
        string? phoneNumber,
        EvolutionConnectionSnapshot currentSnapshot,
        CancellationToken cancellationToken)
    {
        var aggregated = currentSnapshot.Merge(GetPendingEvolutionSnapshot(instanceName));

        for (var attempt = 0; attempt < 6 && !aggregated.IsConnected && !aggregated.HasQrPayload; attempt++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken);
            aggregated = aggregated.Merge(GetPendingEvolutionSnapshot(instanceName));

            if (!aggregated.HasQrPayload && attempt == 2)
            {
                aggregated = aggregated.Merge(await ConnectEvolutionInstanceAsync(instanceName, phoneNumber, cancellationToken));
            }
        }

        return aggregated;
    }

    private async Task<EvolutionConnectionSnapshot> GetEvolutionConnectionStateAsync(string instanceName, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildEvolutionApiUrl($"instance/connectionState/{Uri.EscapeDataString(instanceName)}"));
        request.Headers.TryAddWithoutValidation("apikey", ResolveEvolutionApiKey());

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            return EvolutionConnectionSnapshot.Missing(instanceName);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Nao foi possivel consultar o estado da instancia na Evolution Lite.");
        }

        return ParseEvolutionConnectionSnapshot(responseText).WithExists();
    }

    private async Task<string?> GetEvolutionInstanceConnectedPhoneAsync(string instanceName, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildEvolutionApiUrl($"instance/fetchInstances?instanceName={Uri.EscapeDataString(instanceName)}"));
        request.Headers.TryAddWithoutValidation("apikey", ResolveEvolutionApiKey());

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return null;
        }

        using var document = JsonDocument.Parse(responseText);
        var root = NormalizeRoot(document.RootElement);
        var ownerJid = GetString(root, ["ownerJid"], ["instance", "ownerJid"]);
        return string.IsNullOrWhiteSpace(ownerJid)
            ? null
            : NormalizePhoneForWhatsApp(ownerJid);
    }

    private string ResolveEvolutionIntegration()
    {
        return string.IsNullOrWhiteSpace(_evolutionOptions.DefaultIntegration)
            ? "WHATSAPP-BAILEYS"
            : _evolutionOptions.DefaultIntegration.Trim();
    }

    private bool IsEvolutionConfigured()
    {
        return !string.IsNullOrWhiteSpace(_evolutionOptions.BaseUrl) &&
               !string.IsNullOrWhiteSpace(ResolveEvolutionApiKey());
    }

    private string ResolveEvolutionApiKey()
    {
        return Environment.GetEnvironmentVariable("WHATSAPP__EVOLUTION__APIKEY")
            ?? Environment.GetEnvironmentVariable("WHATSAPP__EVOLUTION__API_KEY")
            ?? _evolutionOptions.ApiKey
            ?? throw new InvalidOperationException("A Evolution Lite ainda nao tem API key configurada no servidor.");
    }

    private Uri BuildEvolutionApiUrl(string relativePath)
    {
        var baseUrl = Environment.GetEnvironmentVariable("WHATSAPP__EVOLUTION__BASEURL")
            ?? Environment.GetEnvironmentVariable("WHATSAPP__EVOLUTION__BASE_URL")
            ?? _evolutionOptions.BaseUrl;

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("A Evolution Lite ainda nao tem base URL configurada no servidor.");
        }

        var normalizedBaseUrl = baseUrl.TrimEnd('/') + "/";
        return new Uri(new Uri(normalizedBaseUrl, UriKind.Absolute), relativePath);
    }

    private string BuildEvolutionWebhookUrl(string? instanceName, string? webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(instanceName) || string.IsNullOrWhiteSpace(webhookSecret))
        {
            throw new InvalidOperationException("A instancia da unidade ainda nao tem URL de webhook pronta.");
        }

        var publicBaseUrl = ResolvePublicBaseUrl()
            ?? throw new InvalidOperationException("A URL publica do ZeroPaper ainda nao esta configurada.");

        return $"{publicBaseUrl.TrimEnd('/')}/api/integrations/whatsapp/evolution/{Uri.EscapeDataString(instanceName)}/events?key={Uri.EscapeDataString(webhookSecret)}";
    }

    private static ParsedInboundMessage ParseZApiInboundMessage(JsonElement rootElement)
    {
        var root = NormalizeRoot(rootElement);
        var phone = GetString(root,
            ["phone"],
            ["chatId"],
            ["from"],
            ["sender", "phone"]);
        var isGroup = GetBool(root, ["isGroup"]);

        if (!isGroup && phone?.Contains("@g.us", StringComparison.OrdinalIgnoreCase) == true)
        {
            isGroup = true;
        }

        var fromMe = GetBool(root, ["fromMe"]);
        var text = GetString(root,
            ["text", "message"],
            ["text", "body"],
            ["text"],
            ["message", "text"],
            ["body"],
            ["caption"]);
        var messageType = GetString(root, ["type"], ["messageType"]) ?? "text";
        var hasReceiptMedia = IsReceiptMediaType(messageType);
        var normalizedText = StripHtml(text ?? string.Empty);
        if (string.IsNullOrWhiteSpace(normalizedText) && hasReceiptMedia)
        {
            normalizedText = "[Comprovante recebido pelo WhatsApp]";
        }

        return new ParsedInboundMessage
        {
            Phone = phone ?? string.Empty,
            CustomerName = GetString(root, ["senderName"], ["pushName"], ["sender", "pushName"], ["sender", "name"]),
            ExternalMessageId = GetString(root, ["messageId"], ["id"], ["message", "id"]),
            Message = normalizedText,
            MessageType = messageType,
            IsIgnored = string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(normalizedText) || isGroup || fromMe
        };
    }

    private static ParsedInboundMessage ParseEvolutionInboundMessage(JsonElement data)
    {
        var directJid = ResolveEvolutionDirectChatJid(
            GetString(data, ["key", "remoteJid"]),
            GetString(data, ["key", "remoteJidAlt"]),
            GetString(data, ["key", "participant"]),
            GetString(data, ["participant"]),
            GetString(data, ["sender"], ["sender", "id"]));
        var fromMe = GetBool(data, ["key", "fromMe"]);
        var text = GetString(
            data,
            ["message", "conversation"],
            ["message", "extendedTextMessage", "text"],
            ["message", "imageMessage", "caption"],
            ["message", "videoMessage", "caption"],
            ["message", "documentMessage", "caption"]);
        var messageType = GetString(data, ["messageType"]) ??
                          (TryGetNestedElement(data, out _, "message", "imageMessage")
                              ? "imageMessage"
                              : TryGetNestedElement(data, out _, "message", "documentMessage")
                                  ? "documentMessage"
                                  : "text");
        var isSupportedMessageType = IsSupportedEvolutionInboundMessageType(messageType);
        var hasReceiptMedia = IsReceiptMediaType(messageType);
        var normalizedText = StripHtml(text ?? string.Empty);
        if (string.IsNullOrWhiteSpace(normalizedText) && hasReceiptMedia)
        {
            normalizedText = "[Comprovante recebido pelo WhatsApp]";
        }

        return new ParsedInboundMessage
        {
            Phone = directJid,
            CustomerName = GetString(data, ["pushName"]),
            ExternalMessageId = GetString(data, ["key", "id"]),
            Message = normalizedText,
            MessageType = messageType,
            OccurredAtUtc = ConvertEpochSecondsToUtc(GetLong(data, ["messageTimestamp"])) ?? DateTime.MinValue,
            IsEvolutionMessage = true,
            IsIgnored = string.IsNullOrWhiteSpace(directJid) ||
                        string.IsNullOrWhiteSpace(normalizedText) ||
                        fromMe ||
                        !isSupportedMessageType
        };
    }

    private static string ResolveEvolutionDirectChatJid(params string?[] candidates)
    {
        var phoneJid = candidates.FirstOrDefault(IsEvolutionPhoneChatJid);
        if (!string.IsNullOrWhiteSpace(phoneJid))
        {
            return phoneJid;
        }

        return candidates.FirstOrDefault(IsEvolutionLidChatJid) ?? string.Empty;
    }

    private static bool IsEvolutionPhoneChatJid(string? jid)
    {
        return !string.IsNullOrWhiteSpace(jid) &&
               jid.EndsWith("@s.whatsapp.net", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEvolutionLidChatJid(string? jid)
    {
        return !string.IsNullOrWhiteSpace(jid) &&
               jid.EndsWith("@lid", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedEvolutionInboundMessageType(string messageType)
    {
        return messageType.Equals("text", StringComparison.OrdinalIgnoreCase) ||
               messageType.Equals("conversation", StringComparison.OrdinalIgnoreCase) ||
               messageType.Equals("extendedTextMessage", StringComparison.OrdinalIgnoreCase) ||
               messageType.Equals("imageMessage", StringComparison.OrdinalIgnoreCase) ||
               messageType.Equals("videoMessage", StringComparison.OrdinalIgnoreCase) ||
               messageType.Equals("documentMessage", StringComparison.OrdinalIgnoreCase);
    }

    private static EvolutionEnvelope ParseEvolutionEnvelope(JsonElement rootElement)
    {
        var root = NormalizeRoot(rootElement);
        return new EvolutionEnvelope
        {
            Event = GetString(
                root,
                ["event"],
                ["type"],
                ["eventType"],
                ["eventName"],
                ["webhook", "event"]) ?? string.Empty,
            Instance = GetString(
                root,
                ["instance"],
                ["instanceName"],
                ["instance", "instanceName"],
                ["sender", "instance"]) ?? string.Empty,
            Data = TryGetNestedElement(root, out var data, "data")
                ? data
                : TryGetNestedElement(root, out var payload, "payload")
                    ? payload
                    : root
        };
    }

    private static ParsedMessageStatus ExtractZApiMessageStatus(JsonElement rootElement)
    {
        var root = NormalizeRoot(rootElement);
        var ids = new List<string>();

        if (TryGetNestedElement(root, out var idsElement, "ids") && idsElement.ValueKind == JsonValueKind.Array)
        {
            ids.AddRange(idsElement.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString()!)
                .Where(item => !string.IsNullOrWhiteSpace(item)));
        }

        var singleId = GetString(root, ["id"]);
        if (!string.IsNullOrWhiteSpace(singleId) && !ids.Contains(singleId))
        {
            ids.Add(singleId);
        }

        return new ParsedMessageStatus
        {
            Ids = ids,
            Status = GetString(root, ["status"]),
            MomentUtc = ConvertEpochToUtc(GetLong(root, ["momment"], ["moment"]))
        };
    }

    private static string? ExtractZApiConnectionPhone(JsonElement rootElement)
    {
        var root = NormalizeRoot(rootElement);
        var phone = GetString(root, ["phone"], ["connectedPhone"], ["value"], ["instance", "phone"]);
        return string.IsNullOrWhiteSpace(phone) ? null : NormalizePhoneForWhatsApp(phone);
    }

    private static string? ExtractEvolutionConnectionPhone(JsonElement data)
    {
        var phone = GetString(data, ["wuid"], ["instance", "wuid"], ["phone"]);
        return string.IsNullOrWhiteSpace(phone) ? null : NormalizePhoneForWhatsApp(phone);
    }

    private static EvolutionConnectionSnapshot ParseEvolutionConnectionSnapshot(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return new EvolutionConnectionSnapshot();
        }

        using var document = JsonDocument.Parse(responseText);
        var root = NormalizeRoot(document.RootElement);
        var instance = TryGetNestedElement(root, out var instanceElement, "instance") ? instanceElement : root;
        var qrcode = TryGetNestedElement(root, out var qrElement, "qrcode") ? qrElement : default;
        var qrPayload = qrcode.ValueKind == JsonValueKind.Undefined ? root : qrcode;
        var state = GetString(instance, ["state"]);

        return new EvolutionConnectionSnapshot
        {
            State = state,
            IsConnected = string.Equals(state, "open", StringComparison.OrdinalIgnoreCase),
            ConnectedPhone = ExtractEvolutionConnectionPhone(instance),
            QrCodeBase64 = GetString(qrPayload, ["base64"], ["qrcode", "base64"], ["qrCode", "base64"], ["data", "base64"]),
            QrCodeText = GetString(qrPayload, ["code"], ["qr"], ["value"]),
            PairingCode = GetString(qrPayload, ["pairingCode"])
        };
    }

    private static EvolutionConnectionSnapshot ParseEvolutionQrCodeSnapshot(JsonElement data)
    {
        var root = NormalizeRoot(data);
        var qrcode = TryGetNestedElement(root, out var qrElement, "qrcode")
            ? qrElement
            : TryGetNestedElement(root, out var qrCodeElement, "qrCode")
                ? qrCodeElement
                : root;
        var state = GetString(root, ["state"], ["instance", "state"]) ?? "connecting";

        return new EvolutionConnectionSnapshot
        {
            State = state,
            IsConnected = string.Equals(state, "open", StringComparison.OrdinalIgnoreCase),
            ConnectedPhone = ExtractEvolutionConnectionPhone(root),
            QrCodeBase64 = GetString(qrcode, ["base64"], ["qrcode", "base64"], ["qrCode", "base64"], ["data", "base64"]),
            QrCodeText = GetString(qrcode, ["code"], ["qr"], ["value"], ["qrcode"], ["qrCode", "code"], ["data", "code"]),
            PairingCode = GetString(qrcode, ["pairingCode"], ["qrcode", "pairingCode"], ["qrCode", "pairingCode"])
        };
    }

    private static string NormalizeEvolutionEventName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().ToLowerInvariant().Replace("_", ".").Replace("-", ".");
    }

    private static EvolutionConnectionSnapshot GetPendingEvolutionSnapshot(string? instanceName)
    {
        if (string.IsNullOrWhiteSpace(instanceName))
        {
            return new EvolutionConnectionSnapshot();
        }

        return PendingEvolutionSnapshots.TryGetValue(instanceName, out var snapshot)
            ? snapshot
            : new EvolutionConnectionSnapshot();
    }

    private static string TruncateWebhookPayload(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= 900
            ? value
            : $"{value[..900]}...";
    }

    private static void StoreEvolutionSnapshot(string? instanceName, EvolutionConnectionSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(instanceName))
        {
            return;
        }

        PendingEvolutionSnapshots[instanceName] = snapshot.WithExists();
    }

    private static JsonElement NormalizeRoot(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0
            ? element[0]
            : element;
    }

    private static string? GetString(JsonElement element, params string[][] paths)
    {
        foreach (var path in paths)
        {
            if (!TryGetNestedElement(element, out var current, path))
            {
                continue;
            }

            if (current.ValueKind == JsonValueKind.String)
            {
                var value = current.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            if (current.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return current.ToString();
            }
        }

        return null;
    }

    private static bool GetBool(JsonElement element, params string[][] paths)
    {
        foreach (var path in paths)
        {
            if (!TryGetNestedElement(element, out var current, path))
            {
                continue;
            }

            if (current.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (current.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (current.ValueKind == JsonValueKind.String && bool.TryParse(current.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return false;
    }

    private static long? GetLong(JsonElement element, params string[][] paths)
    {
        foreach (var path in paths)
        {
            if (!TryGetNestedElement(element, out var current, path))
            {
                continue;
            }

            if (current.ValueKind == JsonValueKind.Number && current.TryGetInt64(out var value))
            {
                return value;
            }

            if (current.ValueKind == JsonValueKind.String && long.TryParse(current.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static bool TryGetNestedElement(JsonElement element, out JsonElement current, params string[] path)
    {
        current = element;

        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizePhoneForWhatsApp(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length is 10 or 11)
        {
            return $"55{digits}";
        }

        return digits;
    }

    private static string NormalizeEvolutionChatIdentifier(string value)
    {
        return NormalizePhoneForWhatsApp(value);
    }

    private static bool IsDuplicateWhatsAppConversationException(DbUpdateException exception)
    {
        var message = exception.ToString();
        return message.Contains("IX_whatsappconversations_CompanyId_ExternalPhone", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeConnectionPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var normalized = NormalizePhoneForWhatsApp(phone);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Length < 12)
        {
            throw new ArgumentException("Informe o numero com DDD para gerar o pareamento do WhatsApp.", nameof(phone));
        }

        return normalized;
    }

    private static DateTime? ConvertEpochToUtc(long? epochMillis)
    {
        if (!epochMillis.HasValue || epochMillis.Value <= 0)
        {
            return null;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(epochMillis.Value).UtcDateTime;
        }
        catch
        {
            return null;
        }
    }

    private static DateTime? ConvertEpochSecondsToUtc(long? epochSeconds)
    {
        if (!epochSeconds.HasValue)
        {
            return null;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(epochSeconds.Value).UtcDateTime;
        }
        catch
        {
            return null;
        }
    }

    private static string NormalizeMessageForWhatsApp(string value)
    {
        var stripped = StripHtml(value);
        stripped = Regex.Replace(stripped, @"\n{3,}", "\n\n").Trim();
        return stripped.Length <= 3500 ? stripped : $"{stripped[..3497]}...";
    }

    private static string StripHtml(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var withoutBreaks = BreakTagRegex().Replace(value, "\n");
        var withoutTags = HtmlTagRegex().Replace(withoutBreaks, string.Empty);
        return WebUtility.HtmlDecode(withoutTags).Trim();
    }

    private static string? ExtractZApiExternalMessageId(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseText);
            var root = NormalizeRoot(document.RootElement);
            return GetString(root, ["zaapId"], ["messageId"], ["id"]);
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractEvolutionExternalMessageId(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseText);
            var root = NormalizeRoot(document.RootElement);
            return GetString(root, ["key", "id"], ["message", "key", "id"], ["id"]);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> BuildDeliveryOrderMessageAsync(
        CustomerOrder order,
        bool isUpdate,
        CancellationToken cancellationToken)
    {
        var companyName = string.IsNullOrWhiteSpace(order.Company.TradeName)
            ? "a unidade"
            : order.Company.TradeName.Trim();

        var customerGreeting = string.IsNullOrWhiteSpace(order.CustomerName)
            ? string.Empty
            : $", {order.CustomerName.Trim()}";
        var isPickupOrder = IsPickupOrder(order);

        var firstParagraph = isUpdate
            ? $"Perfeito{customerGreeting}! Seu delivery foi atualizado em {companyName} ✅"
            : $"Perfeito{customerGreeting}! Seu delivery foi recebido em {companyName} ✅";

        if (isPickupOrder)
        {
            firstParagraph = isUpdate
                ? $"Perfeito{customerGreeting}! Seu pedido para retirada foi atualizado em {companyName}."
                : $"Perfeito{customerGreeting}! Seu pedido para retirada foi recebido em {companyName}.";
        }

        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(order.CustomerName))
        {
            details.Add($"Cliente: {order.CustomerName.Trim()}");
        }

        if (isPickupOrder)
        {
            details.Add("Retirada: no local");
        }

        if (!isPickupOrder && (!string.IsNullOrWhiteSpace(order.DeliveryAddress) || !string.IsNullOrWhiteSpace(order.DeliveryNumber)))
        {
            var address = $"{order.DeliveryAddress?.Trim() ?? string.Empty}, {order.DeliveryNumber?.Trim() ?? string.Empty}".Trim().TrimEnd(',').Trim();
            if (!string.IsNullOrWhiteSpace(order.DeliveryComplement))
            {
                address = $"{address} ({order.DeliveryComplement.Trim()})";
            }

            if (!string.IsNullOrWhiteSpace(address))
            {
                details.Add($"Entrega: {address}");
            }
        }

        var paragraphs = new List<string> { firstParagraph };
        if (details.Count > 0)
        {
            paragraphs.Add(string.Join("\n", details));
        }

        var customerLink = !string.IsNullOrWhiteSpace(order.DeliveryPhone)
            ? await BuildPersonalDeliveryUrlAsync(
                order.CompanyId,
                NormalizePhoneForWhatsApp(order.DeliveryPhone),
                cancellationToken)
            : null;
        if (!isUpdate && !string.IsNullOrWhiteSpace(customerLink))
        {
            paragraphs.Add(
                $"Seu link rapido para proximos pedidos 👇\n{customerLink}\nEle reaproveita os dados deste telefone com seguranca. Confira antes de enviar.");
        }

        if ((order.RequestedPaymentMethod == Domain.Enums.PaymentMethod.Pix ||
             order.PaymentMethod == Domain.Enums.PaymentMethod.Pix) &&
            !string.IsNullOrWhiteSpace(order.Company.AiAssistantPixKey))
        {
            var pixLines = new List<string>
            {
                $"Pix da unidade 💳 {order.Company.AiAssistantPixKey.Trim()}"
            };

            if (!string.IsNullOrWhiteSpace(order.Company.AiAssistantPixReceiverName))
            {
                pixLines.Add($"Recebedor: {order.Company.AiAssistantPixReceiverName.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(order.Company.AiAssistantPixMessage))
            {
                pixLines.Add(order.Company.AiAssistantPixMessage.Trim());
            }

            pixLines.Add(isPickupOrder
                ? "A retirada segue depois que a equipe conferir o comprovante manualmente."
                : "A entrega segue depois que a equipe conferir o comprovante manualmente.");
            pixLines.Add("Comprovante recebido nao confirma pagamento automaticamente.");
            paragraphs.Add(string.Join("\n", pixLines));
        }

        paragraphs.Add("A equipe acompanha a entrega por aqui. Se precisar de algo, pode responder esta conversa 😊");
        if (isPickupOrder)
        {
            var lastOperationalParagraphIndex = paragraphs.FindLastIndex(item => item.Contains("entrega por aqui", StringComparison.OrdinalIgnoreCase));
            if (lastOperationalParagraphIndex >= 0)
            {
                paragraphs[lastOperationalParagraphIndex] = "A equipe acompanha o preparo por aqui. Se precisar de algo, pode responder esta conversa.";
            }
        }

        return NormalizeMessageForWhatsApp(string.Join("\n\n", paragraphs));
    }

    private static bool IsPickupOrder(CustomerOrder order)
    {
        return order.DiningTable.IsDeliveryChannel &&
               string.Equals(order.DeliveryAddress, "Retirada no local", StringComparison.OrdinalIgnoreCase) &&
               string.IsNullOrWhiteSpace(order.DeliveryPostalCode);
    }

    private static string BuildFallbackReply(Company company)
    {
        var fallback = StripHtml(company.AiAssistantFallbackMessage);
        if (string.IsNullOrWhiteSpace(fallback) || LooksLikeOldReviewFallback(fallback))
        {
            var companyName = string.IsNullOrWhiteSpace(company.TradeName)
                ? "a unidade"
                : company.TradeName.Trim();

            fallback = string.IsNullOrWhiteSpace(company.AiAssistantOrderingLink)
                ? $"Quero te ajudar certinho 😊 Neste momento o atendimento de {companyName} vai continuar pelo canal oficial da unidade."
                : $"Quero te ajudar certinho 😊 Para continuar com seguranca em {companyName}, use o link oficial da unidade 👇\n{company.AiAssistantOrderingLink.Trim()}";
        }

        return NormalizeMessageForWhatsApp(fallback);
    }

    private static string BuildOrderTrackingReply(DeliveryCustomerContext deliveryContext)
    {
        if (!deliveryContext.HasKnownOrder)
        {
            return "Nao encontrei nenhum pedido para este numero.";
        }

        var status = FormatCustomerOrderTrackingStatus(deliveryContext.ActiveOrderStatus);
        var linkLine = string.IsNullOrWhiteSpace(deliveryContext.TrackingLink)
            ? string.Empty
            : $"\n\nAcompanhe por aqui:\n{deliveryContext.TrackingLink}";

        if (deliveryContext.ActiveOrderNumber is > 0)
        {
            return $"Encontrei seu pedido #{deliveryContext.ActiveOrderNumber}. Status: {status}.{linkLine}";
        }

        return $"Encontrei seu pedido. Status: {status}.{linkLine}";
    }

    private static string FormatCustomerOrderTrackingStatus(string? status)
    {
        return status switch
        {
            nameof(OrderStatus.Pending) => "recebido",
            nameof(OrderStatus.InKitchen) => "em preparo",
            nameof(OrderStatus.Ready) => "pronto",
            nameof(OrderStatus.Delivered) => "finalizado",
            nameof(OrderStatus.Cancelled) => "cancelado",
            _ => "em andamento"
        };
    }

    private static bool LooksLikeOldReviewFallback(string value)
    {
        var normalized = value.ToLowerInvariant();
        return (normalized.Contains("equipe") &&
                (normalized.Contains("revisar") ||
                 normalized.Contains("revisao") ||
                 normalized.Contains("confirmar")) &&
                normalized.Contains("seguranca")) ||
               normalized.Contains("para esse ponto especifico") ||
               normalized.Contains("siga pelo canal oficial da unidade");
    }

    private static string? BuildCustomerPromptContext(
        DeliveryCustomerContext deliveryContext,
        bool personalLinkAlreadySentRecently,
        bool shouldAllowLinkResend)
    {
        if (string.IsNullOrWhiteSpace(deliveryContext.PromptContext))
        {
            return deliveryContext.PromptContext;
        }

        var context = deliveryContext.PromptContext;
        return context
            + "\nQuando o cliente demonstrar intencao real de pedir, comprar, ver cardapio ou fazer delivery, envie o link completo, mesmo que ele ja tenha aparecido antes."
            + "\nQuando o cliente pedir status, acompanhamento, andamento, pedido atual ou quiser ver um pedido ja enviado, nao envie link de novo pedido."
            + "\nO link de novo pedido serve para comprar de novo; acompanhamento do pedido atual usa somente o link de acompanhamento quando disponivel."
            + "\nNao use a frase \"pelo link que te mandei\" sem mandar o link junto."
            + "\nMantenha a resposta curta, simpatica e com no maximo 1 a 3 emojis.";
    }

    private static string BuildEstimatedWaitContext(Company company)
    {
        var estimates = new List<string>();

        if (company.PickupEstimatedMinutes is > 0)
        {
            estimates.Add($"retirada: {company.PickupEstimatedMinutes} minutos");
        }

        if (company.DeliveryEstimatedMinutes is > 0)
        {
            estimates.Add($"entrega: {company.DeliveryEstimatedMinutes} minutos");
        }

        return estimates.Count == 0
            ? "Tempo estimado nao configurado. Se o cliente perguntar prazo, diga que a unidade ainda nao informou uma estimativa no sistema."
            : "Tempo estimado configurado (" + string.Join("; ", estimates) + "). Responda prazos somente se o cliente perguntar sobre demora, previsao ou espera.";
    }

    private static string BuildPixPromptContext(Company company)
    {
        if (string.IsNullOrWhiteSpace(company.AiAssistantPixKey))
        {
            return "Pix: chave nao configurada. Se perguntarem por Pix, diga que a unidade ainda nao informou a chave no sistema.";
        }

        var receiver = string.IsNullOrWhiteSpace(company.AiAssistantPixReceiverName)
            ? string.Empty
            : $" Recebedor: {company.AiAssistantPixReceiverName.Trim()}.";

        return $"Pix: se o cliente perguntar diretamente, responda curto com a chave {company.AiAssistantPixKey.Trim()}.{receiver} Informe que a entrega ou retirada segue apos a equipe conferir o comprovante manualmente.";
    }

    private static bool HasPersonalDeliveryLinkInHistory(
        IEnumerable<AiAssistantConversationTurnDto> history,
        string? customerLink)
    {
        return !string.IsNullOrWhiteSpace(customerLink) &&
               history.Any(turn =>
                   string.Equals(turn.Role, "assistant", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(turn.Content) &&
                   turn.Content.Contains(customerLink, StringComparison.OrdinalIgnoreCase));
    }

    private static string EnsurePersonalDeliveryLinkWhenNeeded(
        string replyText,
        string inboundMessage,
        DeliveryCustomerContext deliveryContext,
        bool personalLinkAlreadySentRecently,
        bool shouldAllowLinkResend)
    {
        var hasOrderingIntent = HasOrderingIntent(inboundMessage);

        if (string.IsNullOrWhiteSpace(deliveryContext.CustomerLink) ||
            !(hasOrderingIntent || shouldAllowLinkResend) ||
            replyText.Contains(deliveryContext.CustomerLink, StringComparison.OrdinalIgnoreCase))
        {
            return replyText;
        }

        if (personalLinkAlreadySentRecently && !shouldAllowLinkResend && !hasOrderingIntent)
        {
            return replyText;
        }

        var linkIntro = deliveryContext.HasSavedProfile
            ? "Para pedir mais rapido com seus dados salvos, use este link e confira se endereco e telefone continuam certos 👇"
            : "Para fazer o pedido pelo delivery da unidade, use este link 👇";

        return NormalizeMessageForWhatsApp($"{replyText}\n\n{linkIntro}\n{deliveryContext.CustomerLink}");
    }

    private static string BuildSubmittedOrderFollowUpReply(Company company, DeliveryCustomerContext deliveryContext)
    {
        var companyName = string.IsNullOrWhiteSpace(company.TradeName)
            ? "a unidade"
            : company.TradeName.Trim();

        var paragraphs = new List<string>
        {
            $"Perfeito 😊 Se voce ja enviou o pedido pelo site, ele entrou no fluxo operacional de {companyName}.",
            "Agora, qualquer duvida, ajuste ou confirmacao deve ser tratado por aqui no WhatsApp. Me diga o que voce precisa que a equipe confere com seguranca."
        };

        if (deliveryContext.HasKnownOrder && !string.IsNullOrWhiteSpace(deliveryContext.TrackingLink))
        {
            paragraphs.Add($"Para acompanhar o pedido atual, use este link:\n{deliveryContext.TrackingLink}");
        }
        else if (deliveryContext.HasSavedProfile && !string.IsNullOrWhiteSpace(deliveryContext.CustomerLink))
        {
            paragraphs.Add(
                $"Para os proximos pedidos, este e seu link rapido com os dados salvos deste numero 👇\n{deliveryContext.CustomerLink}\nAntes de enviar, confira se endereco e telefone continuam certos.");
        }

        return NormalizeMessageForWhatsApp(string.Join("\n\n", paragraphs));
    }

    private static string ApplyDeliveryConversationSafetyRules(
        string replyText,
        string inboundMessage,
        Company company,
        DeliveryCustomerContext deliveryContext)
    {
        if (string.IsNullOrWhiteSpace(replyText))
        {
            return replyText;
        }

        var isSubmittedOrderFollowUp = HasSubmittedOrderFollowUpIntent(inboundMessage);
        if (MentionsDisabledEditFlow(replyText) ||
            (isSubmittedOrderFollowUp && MentionsSiteTrackingAfterOrder(replyText)))
        {
            return BuildSubmittedOrderFollowUpReply(company, deliveryContext);
        }

        if (MentionsPriorLinkWithoutUrl(replyText) &&
            !string.IsNullOrWhiteSpace(deliveryContext.CustomerLink))
        {
            var cleanedReply = Regex.Replace(
                replyText,
                @"(?i)\b(pelo\s+)?link\s+que\s+te\s+mandei\b",
                "pelo link abaixo");

            if (!cleanedReply.Contains(deliveryContext.CustomerLink, StringComparison.OrdinalIgnoreCase))
            {
                cleanedReply = $"{cleanedReply.Trim()}\n\n{deliveryContext.CustomerLink}";
            }

            return NormalizeMessageForWhatsApp(cleanedReply);
        }

        return NormalizeMessageForWhatsApp(replyText);
    }

    private static bool MentionsPriorLinkWithoutUrl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = NormalizeForIntent(text);
        return normalized.Contains("link que te mandei") ||
               normalized.Contains("pelo link que te mandei");
    }

    private static bool MentionsDisabledEditFlow(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = NormalizeForIntent(text);
        return normalized.Contains("3 minutos") ||
               normalized.Contains("tres minutos") ||
               normalized.Contains("link de edicao") ||
               normalized.Contains("link de editar") ||
               normalized.Contains("editar pelo link") ||
               normalized.Contains("edicao pelo link") ||
               normalized.Contains("janela de edicao") ||
               normalized.Contains("pode ser alterado por") ||
               normalized.Contains("alterar pelo link");
    }

    private static bool MentionsSiteTrackingAfterOrder(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = NormalizeForIntent(text);
        return (normalized.Contains("acompanhar") || normalized.Contains("acompanhe")) &&
               (normalized.Contains("pelo site") ||
                normalized.Contains("no site") ||
                normalized.Contains("proprio site") ||
                normalized.Contains("pelo proprio site"));
    }

    private static bool HasExplicitLinkRequest(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var normalized = NormalizeForIntent(message);
        return normalized.Contains("link") ||
               normalized.Contains("manda de novo") ||
               normalized.Contains("envia de novo") ||
               normalized.Contains("reenvia") ||
               normalized.Contains("reenviar") ||
               normalized.Contains("nao achei") ||
               normalized.Contains("nao encontrei") ||
               normalized.Contains("perdi");
    }

    private static bool HasOrderingIntent(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var normalized = NormalizeForIntent(message);
        return normalized.Contains("quero pedir") ||
               normalized.Contains("fazer pedido") ||
               normalized.Contains("fazer um pedido") ||
               normalized.Contains("novo pedido") ||
               normalized.Contains("pedir") ||
               normalized.Contains("cardap") ||
               normalized.Contains("delivery") ||
               normalized.Contains("entrega") ||
               normalized.Contains("comprar") ||
               normalized.Contains("quero um") ||
               normalized.Contains("quero pedir");
    }

    private static bool HasSubmittedOrderFollowUpIntent(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var normalized = NormalizeForIntent(message);
        return normalized.Contains("ja pedi") ||
               normalized.Contains("pedido feito") ||
               normalized.Contains("fiz o pedido") ||
               normalized.Contains("acabei de pedir") ||
               normalized.Contains("acabei de enviar") ||
               normalized.Contains("enviei o pedido") ||
               normalized.Contains("finalizei") ||
               normalized.Contains("conclui") ||
               normalized.Contains("e agora") ||
               normalized.Contains("acompanhar pedido") ||
               normalized.Contains("meu pedido");
    }

    private static bool HasOrderTrackingIntent(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var normalized = NormalizeForIntent(message);
        var mentionsOrder = normalized.Contains("pedido") ||
                            normalized.Contains("entrega") ||
                            normalized.Contains("delivery");

        return (mentionsOrder && (
                   normalized.Contains("acompanhar") ||
                   normalized.Contains("acompanhe") ||
                   normalized.Contains("status") ||
                   normalized.Contains("situacao") ||
                   normalized.Contains("andamento") ||
                   normalized.Contains("pedido atual") ||
                   normalized.Contains("ver pedido") ||
                   normalized.Contains("ver meu pedido") ||
                   normalized.Contains("link de acompanhamento") ||
                   normalized.Contains("link do acompanhamento") ||
                   normalized.Contains("rastrear") ||
                   normalized.Contains("rastreio") ||
                   normalized.Contains("cade") ||
                   normalized.Contains("onde esta") ||
                   normalized.Contains("como esta"))) ||
               normalized.Contains("meu pedido chegou") ||
               normalized.Contains("meu pedido saiu") ||
               normalized.Contains("quero acompanhar");
    }

    private static string NormalizeForIntent(string value)
    {
        var normalized = value
            .ToLowerInvariant()
            .Replace("á", "a", StringComparison.Ordinal)
            .Replace("à", "a", StringComparison.Ordinal)
            .Replace("ã", "a", StringComparison.Ordinal)
            .Replace("â", "a", StringComparison.Ordinal)
            .Replace("é", "e", StringComparison.Ordinal)
            .Replace("ê", "e", StringComparison.Ordinal)
            .Replace("í", "i", StringComparison.Ordinal)
            .Replace("ó", "o", StringComparison.Ordinal)
            .Replace("ô", "o", StringComparison.Ordinal)
            .Replace("õ", "o", StringComparison.Ordinal)
            .Replace("ú", "u", StringComparison.Ordinal)
            .Replace("ç", "c", StringComparison.Ordinal);

        return normalized;
    }

    private static bool IsReceiptMediaType(string? messageType)
    {
        if (string.IsNullOrWhiteSpace(messageType))
        {
            return false;
        }

        var normalized = messageType.ToLowerInvariant();
        return normalized.Contains("image", StringComparison.Ordinal) ||
               normalized.Contains("document", StringComparison.Ordinal);
    }

    private async Task<string?> BuildPersonalDeliveryUrlAsync(
        Guid companyId,
        string normalizedPhone,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return null;
        }

        var baseUrl = ResolvePublicBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        var shortCode = await _deliveryCustomerLinkService.GetOrCreateShortCodeForCustomerAsync(
            companyId,
            normalizedPhone,
            cancellationToken);

        return string.IsNullOrWhiteSpace(shortCode)
            ? null
            : $"{baseUrl.TrimEnd('/')}/d/{Uri.EscapeDataString(shortCode)}";
    }

    private async Task<string?> BuildPersonalDeliveryTrackingUrlAsync(
        Guid companyId,
        string normalizedPhone,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return null;
        }

        var baseUrl = ResolvePublicBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        var shortCode = await _deliveryCustomerLinkService.GetOrCreateShortCodeForCustomerAsync(
            companyId,
            normalizedPhone,
            cancellationToken);

        return string.IsNullOrWhiteSpace(shortCode)
            ? null
            : $"{baseUrl.TrimEnd('/')}/acompanhar/{Uri.EscapeDataString(shortCode)}";
    }

    private string? BuildGenericDeliveryUrl(string publicCode)
    {
        if (string.IsNullOrWhiteSpace(publicCode))
        {
            return null;
        }

        var baseUrl = ResolvePublicBaseUrl();
        return string.IsNullOrWhiteSpace(baseUrl)
            ? null
            : $"{baseUrl.TrimEnd('/')}/q/{publicCode.Trim().ToLowerInvariant()}";
    }

    private string? ResolvePublicBaseUrl()
    {
        var configured = Environment.GetEnvironmentVariable("PUBLIC_APP_BASE_URL")
            ?? _publicAppOptions.BaseUrl;

        return string.IsNullOrWhiteSpace(configured)
            ? null
            : configured.Trim();
    }

    private static bool SecretsMatch(string? expected, string? actual)
    {
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static string CreateWebhookSecret()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    }

    private static string BuildEvolutionInstanceName(Company company)
    {
        return $"ZP-{company.Id:N}".ToUpperInvariant();
    }

    private static string NormalizeEvolutionInstanceName(string value)
    {
        var normalized = new string(value
            .Trim()
            .ToUpperInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());

        while (normalized.Contains("--", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        }

        normalized = normalized.Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "ZEROPAPER-DELIVERY" : normalized;
    }

    private string? UnprotectOrNull(IDataProtector protector, string? cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
        {
            return null;
        }

        try
        {
            return protector.Unprotect(cipherText);
        }
        catch
        {
            return null;
        }
    }

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BreakTagRegex();

    [GeneratedRegex("<.*?>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    private sealed class ParsedInboundMessage
    {
        public string Phone { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? ExternalMessageId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string MessageType { get; set; } = "text";
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
        public bool IsEvolutionMessage { get; set; }
        public bool IsIgnored { get; set; }
    }

    private sealed class ParsedMessageStatus
    {
        public List<string> Ids { get; set; } = [];
        public string? Status { get; set; }
        public DateTime? MomentUtc { get; set; }
    }

    private sealed class SendMessageResult
    {
        public SendMessageResult(bool succeeded, string? externalMessageId, string status)
        {
            Succeeded = succeeded;
            ExternalMessageId = externalMessageId;
            Status = status;
        }

        public bool Succeeded { get; }
        public string? ExternalMessageId { get; }
        public string Status { get; }

        public static SendMessageResult Failed(string status) => new(false, null, status);
    }

    private sealed class DeliveryCustomerContext
    {
        public DeliveryCustomerContext(
            string? promptContext,
            string? customerLink,
            bool hasSavedProfile,
            bool hasKnownOrder,
            int? activeOrderNumber = null,
            string? activeOrderStatus = null,
            string? trackingLink = null)
        {
            PromptContext = promptContext;
            CustomerLink = customerLink;
            HasSavedProfile = hasSavedProfile;
            HasKnownOrder = hasKnownOrder;
            ActiveOrderNumber = activeOrderNumber;
            ActiveOrderStatus = activeOrderStatus;
            TrackingLink = trackingLink;
        }

        public string? PromptContext { get; }
        public string? CustomerLink { get; }
        public bool HasSavedProfile { get; }
        public bool HasKnownOrder { get; }
        public int? ActiveOrderNumber { get; }
        public string? ActiveOrderStatus { get; }
        public string? TrackingLink { get; }

        public static DeliveryCustomerContext Empty { get; } = new(null, null, false, false);
    }

    private sealed class EvolutionEnvelope
    {
        public string Event { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
        public JsonElement Data { get; set; }
    }

    private sealed class EvolutionConnectionSnapshot
    {
        public bool InstanceExists { get; set; }
        public string? State { get; set; }
        public bool IsConnected { get; set; }
        public string? ConnectedPhone { get; set; }
        public string? QrCodeBase64 { get; set; }
        public string? QrCodeText { get; set; }
        public string? PairingCode { get; set; }
        public bool HasQrPayload => !string.IsNullOrWhiteSpace(QrCodeBase64) || !string.IsNullOrWhiteSpace(QrCodeText);

        public static EvolutionConnectionSnapshot Missing(string _)
        {
            return new EvolutionConnectionSnapshot
            {
                InstanceExists = false,
                State = "missing"
            };
        }

        public EvolutionConnectionSnapshot WithExists()
        {
            InstanceExists = true;
            return this;
        }

        public EvolutionConnectionSnapshot Merge(EvolutionConnectionSnapshot other)
        {
            return new EvolutionConnectionSnapshot
            {
                InstanceExists = InstanceExists || other.InstanceExists,
                State = other.State ?? State,
                IsConnected = IsConnected || other.IsConnected,
                ConnectedPhone = other.ConnectedPhone ?? ConnectedPhone,
                QrCodeBase64 = other.QrCodeBase64 ?? QrCodeBase64,
                QrCodeText = other.QrCodeText ?? QrCodeText,
                PairingCode = other.PairingCode ?? PairingCode
            };
        }
    }
}
