using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.DTOs.Workspace;
using ZeroPaper.Services.Interfaces;
using ZeroPaper.Services.Models;

namespace ZeroPaper.Services;

public class AiAssistantService : IAiAssistantService
{
    private const string WhatsAppInstanceTokenProtectorPurpose = "ZeroPaper.WhatsApp.InstanceToken.v1";
    private const string WhatsAppAccountTokenProtectorPurpose = "ZeroPaper.WhatsApp.AccountToken.v1";
    private const string WhatsAppWebhookSecretProtectorPurpose = "ZeroPaper.WhatsApp.WebhookSecret.v1";
    private const string ServiceWindowModel = "Horario da unidade";
    private const int ConversationalMinimumMaxOutputTokens = 300;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeZoneInfo ServiceTimeZone = ResolveServiceTimeZone();

    private readonly HttpClient _httpClient;
    private readonly ZeroPaperDbContext _context;
    private readonly OpenAiApiOptions _options;
    private readonly PublicAppOptions _publicAppOptions;
    private readonly EvolutionApiOptions _evolutionOptions;
    private readonly ILogger<AiAssistantService> _logger;
    private readonly IDataProtector _whatsAppInstanceTokenProtector;
    private readonly IDataProtector _whatsAppAccountTokenProtector;
    private readonly IDataProtector _whatsAppWebhookSecretProtector;

    public AiAssistantService(
        HttpClient httpClient,
        ZeroPaperDbContext context,
        IOptions<OpenAiApiOptions> options,
        IOptions<PublicAppOptions> publicAppOptions,
        IOptions<EvolutionApiOptions> evolutionOptions,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<AiAssistantService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _options = options.Value;
        _publicAppOptions = publicAppOptions.Value;
        _evolutionOptions = evolutionOptions.Value;
        _logger = logger;
        _whatsAppInstanceTokenProtector = dataProtectionProvider.CreateProtector(WhatsAppInstanceTokenProtectorPurpose);
        _whatsAppAccountTokenProtector = dataProtectionProvider.CreateProtector(WhatsAppAccountTokenProtectorPurpose);
        _whatsAppWebhookSecretProtector = dataProtectionProvider.CreateProtector(WhatsAppWebhookSecretProtectorPurpose);
    }

    public async Task<AiAssistantSettingsDto> GetSettingsAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var company = await GetCompanyAsync(session.CompanyId, tracking: false, cancellationToken);
        var recentConversations = await GetRecentWhatsAppConversationsAsync(company.Id, cancellationToken);
        return MapSettings(company, IsApiConfigured(), recentConversations);
    }

    public async Task<AiAssistantQuickStatusDto> GetQuickStatusAsync(
        WorkspaceSessionContext session,
        CancellationToken cancellationToken = default)
    {
        var company = await GetCompanyAsync(session.CompanyId, tracking: false, cancellationToken);
        return MapQuickStatus(company);
    }

    public async Task<AiAssistantQuickStatusDto> UpdateQuickStatusAsync(
        WorkspaceSessionContext session,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        var company = await GetCompanyAsync(session.CompanyId, tracking: true, cancellationToken);
        company.SetAiAssistantEnabled(isEnabled);
        company.UpdateWhatsAppIntegration(
            isEnabled,
            company.WhatsAppInstanceId,
            company.WhatsAppInstanceTokenCipherText,
            company.WhatsAppAccountSecurityTokenCipherText,
            company.WhatsAppWebhookSecretCipherText);
        await _context.SaveChangesAsync(cancellationToken);
        return MapQuickStatus(company);
    }

    public async Task<AiAssistantSettingsDto> UpdateSettingsAsync(WorkspaceSessionContext session, UpdateAiAssistantSettingsRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var company = await GetCompanyAsync(session.CompanyId, tracking: true, cancellationToken);

        company.UpdateAiAssistantSettings(
            company.EnableAiAssistant,
            request.Model,
            request.SystemPrompt,
            request.GreetingMessage,
            request.RedirectMessage,
            request.FallbackMessage,
            request.OrderingLink,
            request.PixReceiverName,
            request.PixKey,
            request.PixMessage,
            NormalizeServiceDaysForCompany(request.ServiceDays),
            request.ServiceStartTime,
            request.ServiceEndTime,
            request.MaxOutputTokens);

        var normalizedInstanceId = string.IsNullOrWhiteSpace(request.WhatsAppInstanceId)
            ? null
            : NormalizeEvolutionInstanceName(request.WhatsAppInstanceId);

        var instanceTokenCipher = company.WhatsAppInstanceTokenCipherText;
        var accountTokenCipher = company.WhatsAppAccountSecurityTokenCipherText;

        var webhookSecretCipher = company.WhatsAppWebhookSecretCipherText;

        if (string.IsNullOrWhiteSpace(normalizedInstanceId))
        {
            instanceTokenCipher = null;
            accountTokenCipher = null;
            webhookSecretCipher = null;
        }
        else if (string.IsNullOrWhiteSpace(webhookSecretCipher))
        {
            webhookSecretCipher = _whatsAppWebhookSecretProtector.Protect(CreateWebhookSecret());
        }

        company.UpdateWhatsAppIntegration(
            company.EnableWhatsAppAssistant,
            normalizedInstanceId,
            instanceTokenCipher,
            accountTokenCipher,
            webhookSecretCipher);

        await _context.SaveChangesAsync(cancellationToken);
        var recentConversations = await GetRecentWhatsAppConversationsAsync(company.Id, cancellationToken);
        return MapSettings(company, IsApiConfigured(), recentConversations);
    }

    public async Task<AiAssistantSettingsDto> GenerateTemplateAsync(WorkspaceSessionContext session, CancellationToken cancellationToken = default)
    {
        var company = await GetCompanyAsync(session.CompanyId, tracking: true, cancellationToken);
        var orderingLink = await ResolveOrderingLinkAsync(company, cancellationToken);
        var template = BuildGeneratedTemplate(company, orderingLink);

        company.UpdateAiAssistantSettings(
            company.EnableAiAssistant,
            company.AiAssistantModel,
            template.SystemPrompt,
            template.GreetingMessage,
            template.RedirectMessage,
            template.FallbackMessage,
            orderingLink,
            company.AiAssistantPixReceiverName,
            company.AiAssistantPixKey,
            company.AiAssistantPixMessage,
            company.AiAssistantServiceDays,
            company.AiAssistantServiceStartTime,
            company.AiAssistantServiceEndTime,
            Math.Max(company.AiAssistantMaxOutputTokens, ConversationalMinimumMaxOutputTokens));

        await _context.SaveChangesAsync(cancellationToken);
        var recentConversations = await GetRecentWhatsAppConversationsAsync(company.Id, cancellationToken);
        return MapSettings(company, IsApiConfigured(), recentConversations);
    }

    public async Task<AiAssistantTestResponseDto> TestAssistantAsync(WorkspaceSessionContext session, AiAssistantTestRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Message);

        if (request.Message.Trim().Length > 1600)
        {
            throw new ArgumentException("Use uma mensagem de teste com no maximo 1600 caracteres.", nameof(request.Message));
        }

        var response = await GenerateReplyAsync(
            session.CompanyId,
            "Test",
            request.Message.Trim(),
            history: null,
            cancellationToken: cancellationToken);

        return new AiAssistantTestResponseDto
        {
            Reply = response.Reply,
            Model = response.Model,
            GeneratedAtUtc = response.GeneratedAtUtc
        };
    }

    public async Task<AiAssistantGeneratedReplyDto> GenerateReplyAsync(
        Guid companyId,
        string source,
        string message,
        IReadOnlyList<AiAssistantConversationTurnDto>? history = null,
        string? customerContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (message.Trim().Length > 1600)
        {
            throw new ArgumentException("A mensagem precisa ter no maximo 1600 caracteres.", nameof(message));
        }

        var company = await GetCompanyAsync(companyId, tracking: false, cancellationToken);
        if (TryBuildOutOfServiceReply(company, out var outOfServiceReply))
        {
            return new AiAssistantGeneratedReplyDto
            {
                Reply = outOfServiceReply,
                Model = ServiceWindowModel,
                GeneratedAtUtc = DateTime.UtcNow
            };
        }

        var apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Configure a OPENAI_API_KEY no backend antes de usar a IA da unidade.");
        }

        var selectedModel = string.IsNullOrWhiteSpace(company.AiAssistantModel)
            ? ResolveDefaultModel()
            : company.AiAssistantModel;

        var requestBody = new OpenAiResponsesRequest
        {
            Model = selectedModel,
            MaxOutputTokens = ResolveEffectiveMaxOutputTokens(company),
            Input = BuildConversationInput(company, message.Trim(), history, customerContext)
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "responses");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            await RegisterInteractionAsync(company, selectedModel, source, succeeded: false, cancellationToken);
            _logger.LogWarning(
                "OpenAI request failed for company {CompanyId} on source {Source} with status {StatusCode}. Response: {Response}",
                company.Id,
                source,
                (int)response.StatusCode,
                TruncateForLog(responseContent, 1200));

            throw new InvalidOperationException("A OpenAI rejeitou o atendimento agora. Revise a chave, o modelo e o saldo antes de tentar novamente.");
        }

        var reply = ExtractOutputText(responseContent);
        if (string.IsNullOrWhiteSpace(reply))
        {
            await RegisterInteractionAsync(company, selectedModel, source, succeeded: false, cancellationToken);
            throw new InvalidOperationException("A OpenAI nao devolveu texto util para esta interacao.");
        }

        await RegisterInteractionAsync(company, selectedModel, source, succeeded: true, cancellationToken);

        return new AiAssistantGeneratedReplyDto
        {
            Reply = reply.Trim(),
            Model = selectedModel,
            GeneratedAtUtc = DateTime.UtcNow
        };
    }

    private async Task<Company> GetCompanyAsync(Guid companyId, bool tracking, CancellationToken cancellationToken)
    {
        var query = tracking ? _context.Companies.AsQueryable() : _context.Companies.AsNoTracking();

        return await query.FirstOrDefaultAsync(item => item.Id == companyId && item.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Unidade nao encontrada.");
    }

    private async Task<List<WhatsAppConversationSummaryDto>> GetRecentWhatsAppConversationsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        return await _context.WhatsAppConversations
            .AsNoTracking()
            .Where(item => item.CompanyId == companyId && item.IsActive)
            .OrderByDescending(item => item.LastInteractionAtUtc ?? item.UpdatedAtUtc)
            .Select(item => new WhatsAppConversationSummaryDto
            {
                Id = item.Id,
                ExternalPhone = item.ExternalPhone,
                CustomerName = item.CustomerName,
                LastMessagePreview = item.LastMessagePreview ?? string.Empty,
                LastDirection = item.LastDirection,
                LastIncomingAtUtc = item.LastIncomingAtUtc,
                LastOutgoingAtUtc = item.LastOutgoingAtUtc,
                LastInteractionAtUtc = item.LastInteractionAtUtc,
                MessageCount = item.Messages.Count()
            })
            .Take(8)
            .ToListAsync(cancellationToken);
    }

    private bool IsApiConfigured()
    {
        return !string.IsNullOrWhiteSpace(ResolveApiKey());
    }

    private bool IsEvolutionServerConfigured()
    {
        return !string.IsNullOrWhiteSpace(_evolutionOptions.BaseUrl) &&
               !string.IsNullOrWhiteSpace(ResolveEvolutionApiKey());
    }

    private string? ResolveApiKey()
    {
        return Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? _options.ApiKey;
    }

    private string? ResolveEvolutionApiKey()
    {
        return Environment.GetEnvironmentVariable("WHATSAPP__EVOLUTION__APIKEY")
            ?? Environment.GetEnvironmentVariable("WHATSAPP__EVOLUTION__API_KEY")
            ?? _evolutionOptions.ApiKey;
    }

    private string ResolveDefaultModel()
    {
        return string.IsNullOrWhiteSpace(_options.DefaultModelName)
            ? OpenAiApiOptions.DefaultModel
            : _options.DefaultModelName.Trim();
    }

    private static int ResolveEffectiveMaxOutputTokens(Company company)
    {
        return Math.Clamp(
            Math.Max(company.AiAssistantMaxOutputTokens, ConversationalMinimumMaxOutputTokens),
            80,
            1200);
    }

    private static string? NormalizeServiceDaysForCompany(IReadOnlyCollection<int>? serviceDays)
    {
        if (serviceDays is null)
        {
            return null;
        }

        var normalized = serviceDays
            .Distinct()
            .OrderBy(item => item)
            .ToList();

        if (normalized.Count == 0)
        {
            throw new ArgumentException("Selecione pelo menos um dia de funcionamento.");
        }

        if (normalized.Any(item => item is < 0 or > 6))
        {
            throw new ArgumentException("Os dias de funcionamento precisam ficar entre domingo e sabado.");
        }

        return normalized.Count == 7
            ? null
            : string.Join(',', normalized);
    }

    private static List<int>? ParseServiceDays(string? serviceDays)
    {
        if (string.IsNullOrWhiteSpace(serviceDays))
        {
            return null;
        }

        var days = serviceDays
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => int.TryParse(item, NumberStyles.None, CultureInfo.InvariantCulture, out var day) ? day : -1)
            .Where(item => item is >= 0 and <= 6)
            .Distinct()
            .OrderBy(item => item)
            .ToList();

        return days.Count is 0 or 7 ? null : days;
    }

    private AiAssistantSettingsDto MapSettings(
        Company company,
        bool apiConfigured,
        List<WhatsAppConversationSummaryDto> recentConversations)
    {
        var webhookSecret = UnprotectOrNull(_whatsAppWebhookSecretProtector, company.WhatsAppWebhookSecretCipherText);
        var webhookUrls = BuildWebhookUrls(company.WhatsAppInstanceId, webhookSecret);

        return new AiAssistantSettingsDto
        {
            UnitDisplayName = company.TradeName,
            ApiConfigured = apiConfigured,
            WhatsAppServerConfigured = IsEvolutionServerConfigured(),
            IsEnabled = company.EnableAiAssistant,
            Model = company.AiAssistantModel,
            SystemPrompt = company.AiAssistantSystemPrompt,
            GreetingMessage = company.AiAssistantGreetingMessage,
            RedirectMessage = company.AiAssistantRedirectMessage,
            FallbackMessage = company.AiAssistantFallbackMessage,
            OrderingLink = company.AiAssistantOrderingLink,
            PixReceiverName = company.AiAssistantPixReceiverName,
            PixKey = company.AiAssistantPixKey,
            PixMessage = company.AiAssistantPixMessage,
            ServiceDays = ParseServiceDays(company.AiAssistantServiceDays),
            ServiceStartTime = company.AiAssistantServiceStartTime,
            ServiceEndTime = company.AiAssistantServiceEndTime,
            MaxOutputTokens = company.AiAssistantMaxOutputTokens,
            WhatsAppEnabled = company.EnableWhatsAppAssistant,
            WhatsAppConfigured = !string.IsNullOrWhiteSpace(company.WhatsAppInstanceId) &&
                                 IsEvolutionServerConfigured() &&
                                 !string.IsNullOrWhiteSpace(company.WhatsAppWebhookSecretCipherText),
            WhatsAppInstanceId = company.WhatsAppInstanceId,
            WhatsAppInstanceTokenMasked = null,
            HasWhatsAppAccountSecurityToken = false,
            IsWhatsAppConnected = company.IsWhatsAppConnected,
            WhatsAppConnectedPhone = company.WhatsAppConnectedPhone,
            WhatsAppConnectedAtUtc = company.WhatsAppConnectedAtUtc,
            WhatsAppDisconnectedAtUtc = company.WhatsAppDisconnectedAtUtc,
            WhatsAppLastIncomingAtUtc = company.WhatsAppLastIncomingAtUtc,
            WhatsAppLastOutgoingAtUtc = company.WhatsAppLastOutgoingAtUtc,
            WhatsAppWebhookReceiveUrl = webhookUrls.ReceiveUrl,
            WhatsAppWebhookMessageStatusUrl = webhookUrls.MessageStatusUrl,
            WhatsAppWebhookConnectedUrl = webhookUrls.ConnectedUrl,
            WhatsAppWebhookDisconnectedUrl = webhookUrls.DisconnectedUrl,
            RecentWhatsAppConversations = recentConversations
        };
    }

    private AiAssistantQuickStatusDto MapQuickStatus(Company company)
    {
        return new AiAssistantQuickStatusDto
        {
            IsEnabled = company.EnableAiAssistant,
            IsConfigured = IsApiConfigured() && !string.IsNullOrWhiteSpace(company.AiAssistantOrderingLink)
        };
    }

    private async Task<string?> ResolveOrderingLinkAsync(Company company, CancellationToken cancellationToken)
    {
        var deliveryTable = await _context.DiningTables
            .AsNoTracking()
            .Include(item => item.QrCodeAccess)
            .FirstOrDefaultAsync(
                item => item.CompanyId == company.Id &&
                        item.IsActive &&
                        item.IsDeliveryChannel,
                cancellationToken);

        if (deliveryTable?.QrCodeAccess is not null)
        {
            return BuildAbsoluteOrderingLink(deliveryTable.QrCodeAccess.AccessPath);
        }

        var candidate = await _context.DiningTables
            .AsNoTracking()
            .Include(item => item.QrCodeAccess)
            .Where(item => item.CompanyId == company.Id && item.IsActive)
            .OrderByDescending(item => item.IsDeliveryChannel)
            .ThenByDescending(item =>
                item.Name.ToLower().Contains("delivery") ||
                item.Name.ToLower().Contains("entrega") ||
                item.InternalCode.ToLower().Contains("delivery") ||
                item.InternalCode.ToLower().Contains("entrega"))
            .ThenBy(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate?.QrCodeAccess is null)
        {
            return company.AiAssistantOrderingLink;
        }

        return BuildAbsoluteOrderingLink(candidate.QrCodeAccess.AccessPath);
    }

    private GeneratedTemplate BuildGeneratedTemplate(Company company, string? orderingLink)
    {
        var displayName = string.IsNullOrWhiteSpace(company.TradeName)
            ? company.LegalName
            : company.TradeName;
        var resolvedLink = !string.IsNullOrWhiteSpace(orderingLink)
            ? orderingLink
            : "LINK_OFICIAL_DA_UNIDADE";
        var redirectMessage = !string.IsNullOrWhiteSpace(orderingLink)
            ? $"<p>Claro! Para escolher os itens e finalizar com seguranca em {displayName}, use o link oficial da unidade 👇</p><p><strong>{resolvedLink}</strong></p><p>Por la voce confere itens, entrega e pagamento sem risco de erro.</p>"
            : $"<p>Para seguir com o pedido de {displayName}, deixe configurado o link oficial da unidade para a IA orientar o cliente do jeito certo.</p>";

        return new GeneratedTemplate
        {
            GreetingMessage = $"<p>Ola! Seja bem-vindo a {displayName} 😊</p><p>Me fala como posso te ajudar hoje. Posso tirar duvidas, orientar o delivery e, quando voce quiser pedir, te levo para o link oficial da unidade.</p>",
            RedirectMessage = redirectMessage,
            FallbackMessage = $"<p>Boa pergunta 😊</p><p>Para evitar te passar algo errado, siga pelo link oficial de {displayName} ou me diga como posso te ajudar com o pedido.</p>",
            SystemPrompt =
                $"""
Voce e a atendente virtual oficial da unidade "{displayName}" dentro do ZeroPaper.
Fale sempre em portugues do Brasil.
Seja cordial, convidativa, simpatica, humana e objetiva, como um atendimento de delivery moderno e bom de conversar.
Use emojis leves e uteis para deixar a conversa mais acolhedora: normalmente 1 a 3 por resposta, como 😊, 👋, 🍽️, 🛵, ✅ ou 👇. Nao use emojis em excesso nem repita emoji em toda frase.
Responda primeiro a duvida operacional do cliente com algo util; depois conduza para o proximo passo quando fizer sentido.
Economize tokens: use respostas curtas, com 2 a 4 frases na maioria dos casos. Evite lista seca, texto longo ou resposta com cara de robo.
Pode soar animada e acolhedora, mas sem exagero, sem muitas exclamacoes e sem repetir frases prontas.
Quando fizer sentido, termine com uma pergunta simples para continuar o atendimento.
Seu papel e conversar bem, explicar como o atendimento funciona e conduzir o cliente para o fluxo oficial do site.
Nao responda perguntas detalhadas de cardapio, itens, sabores, precos, taxas ou disponibilidade pelo WhatsApp. Nesses casos, explique com simpatia que o cardapio atualizado fica no link oficial.
Pedidos e pagamentos so podem ser concluidos pelo site oficial do ZeroPaper.
Quando o cliente perguntar sobre pagamento antes de concluir um pedido, explique com leveza que a forma de pagamento e confirmada no fluxo oficial do pedido.
Depois que o cliente informar que ja enviou/concluiu o pedido, nao diga para acompanhar pelo site. Oriente que duvidas, ajustes e acompanhamento devem ser tratados por esta conversa ou pelo atendimento da unidade.
A edicao publica por link apos envio do pedido esta desativada. Nunca cite janela de 3 minutos, link de edicao, editar pelo site ou alterar pedido por link.
Status oficial de atendimento e pedidos agora: {BuildServiceWindowPrompt(company)}
Tempos estimados configurados: {BuildEstimatedWaitPrompt(company)}
Chave Pix configurada: {BuildPixKeyPrompt(company)}
Nao pergunte ao cliente se a unidade pode atender. Use o status oficial acima.
Quando o cliente demonstrar intencao real de pedir, ver cardapio, fazer delivery ou comprar, envie este link literal ao cliente: {resolvedLink}
Nao use a frase "pelo link que te mandei" sem reenviar o link. Quando houver intencao real de pedir, envie o link literal novamente.
Se o cliente perguntar sobre Pix, responda de forma curta com a chave Pix da unidade apenas quando ela estiver configurada. Se nao houver chave Pix configurada, diga que a unidade ainda nao informou a chave no sistema.
Toda conferencia de Pix e manual pela equipe da unidade. Nao analise comprovante, nao valide pagamento, nao solicite frase de confirmacao e nao diga que uma confirmacao foi registrada automaticamente.
Ao falar de Pix, diga que a entrega ou retirada segue depois que a equipe conferir o comprovante manualmente.
Se o cliente disser que acabou de enviar o pedido, confirme de forma cordial que a unidade recebeu o envio pelo site. Se ele precisar corrigir algo, oriente a responder nesta conversa para a equipe conferir com seguranca; nao prometa edicao automatica por link.
Nao invente itens, precos, taxas, disponibilidade, prazo, confirmacao de pagamento ou confirmacao de pedido.
Nao confirme pagamento automaticamente.
Quando orientar Pix, deixe claro que a loja fara a conferencia manual antes da entrega.
Nao monte ou feche pedidos pela conversa.
Ajude o cliente, converse com educacao e so depois conduza para o site quando fizer sentido.
Use a saudacao de boas-vindas apenas na primeira mensagem da conversa.
Nao repita apresentacao, boas-vindas ou o nome da unidade em todas as respostas.
Depois da primeira abordagem, continue o atendimento de forma direta, cordial, natural e sem redundancia.
Use a mensagem de fallback apenas quando realmente nao houver informacao suficiente ou quando houver risco de afirmar algo errado.
As mensagens da unidade podem vir em HTML simples, mas sua resposta ao cliente deve sair limpa, natural e facil de entender.
Se o cliente parecer em duvida, seja prestativa: explique o caminho, tranquilize e ofereca o proximo passo sem pressionar.
""",
        };
    }

    private static List<OpenAiResponsesMessage> BuildConversationInput(
        Company company,
        string userMessage,
        IReadOnlyList<AiAssistantConversationTurnDto>? history,
        string? customerContext)
    {
        var input = new List<OpenAiResponsesMessage>
        {
            BuildMessage("system", BuildSystemPrompt(company))
        };

        if (history is not null)
        {
            var transcript = history
                .Where(item => !string.IsNullOrWhiteSpace(item.Role) && !string.IsNullOrWhiteSpace(item.Content))
                .TakeLast(10)
                .Select(turn =>
                {
                    var roleLabel = string.Equals(turn.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                        ? "Atendente"
                        : "Cliente";

                    return $"{roleLabel}: {turn.Content.Trim()}";
                })
                .ToList();

            if (transcript.Count > 0)
            {
                input.Add(BuildMessage(
                    "user",
                    "Contexto recente da conversa para manter continuidade sem repetir saudacao, apresentacao ou link ja enviado:\n"
                    + string.Join("\n", transcript)));
            }
            else
            {
                input.Add(BuildMessage(
                    "user",
                    "Esta e a primeira mensagem registrada desta conversa. Abra com simpatia, use um emoji leve se fizer sentido, responda a intencao do cliente e evite texto longo."));
            }
        }
        else
        {
            input.Add(BuildMessage(
                "user",
                "Esta e a primeira mensagem da conversa. Abra com simpatia, use um emoji leve se fizer sentido, responda a intencao do cliente e evite texto longo."));
        }

        if (!string.IsNullOrWhiteSpace(customerContext))
        {
            input.Add(BuildMessage(
                "user",
                "Contexto seguro do cliente identificado pelo WhatsApp. Use apenas para personalizar a orientacao e nao exponha endereco completo na conversa:\n"
                + customerContext.Trim()));
        }

        input.Add(BuildMessage("user", userMessage));
        return input;
    }

    private static OpenAiResponsesMessage BuildMessage(string role, string text)
    {
        return new OpenAiResponsesMessage
        {
            Role = role,
            Content =
            [
                new OpenAiResponsesContent
                {
                    Type = "input_text",
                    Text = text
                }
            ]
        };
    }

    private static string BuildSystemPrompt(Company company)
    {
        return $"""
Voce e o atendente digital da unidade "{company.TradeName}" dentro do ZeroPaper.
Fale sempre em portugues do Brasil.
Seja cordial, convidativo, leve, humano e organizado, como um bom atendimento de delivery pelo WhatsApp.
Use emojis com naturalidade, como um atendimento de delivery moderno: normalmente 1 a 3 emojis relevantes por resposta. Prefira emojis simples como 😊, 👋, 🍽️, 🛵, ✅ e 👇. Nao transforme a resposta em bloco de emojis.
Responda em paragrafos curtos e naturais. Evite textao, lista seca e frases com cara de robo.
Na primeira resposta da conversa, seja acolhedor sem exagerar; depois siga a conversa sem repetir boas-vindas.
Responda primeiro a duvida operacional do cliente com a melhor orientacao valida possivel e so depois conduza para o site.
Nao responda perguntas detalhadas de cardapio, itens, sabores, precos, taxas ou disponibilidade pelo WhatsApp. Se o cliente pedir cardapio ou quiser escolher itens, envie o link oficial.
Economize tokens: use respostas curtas, com 2 a 4 frases na maioria dos casos.
Use um tom simpatico e prestativo. Pode ser animado na medida certa, mas sem muitas exclamacoes.
Quando fizer sentido, termine com uma pergunta simples que ajude o cliente a seguir.
Nao invente itens, precos, taxa, disponibilidade, enderecos ou prazos.
Pedidos e pagamentos so podem ser concluidos pelo site oficial do ZeroPaper.
Status oficial de atendimento e pedidos agora: {BuildServiceWindowPrompt(company)}
Tempos estimados configurados: {BuildEstimatedWaitPrompt(company)}
Chave Pix configurada: {BuildPixKeyPrompt(company)}
Nao pergunte ao cliente se a unidade esta atendendo. Use o status oficial acima.
Se o cliente perguntar sobre pagamento antes de concluir um pedido, diga com clareza que a forma de pagamento e confirmada no fluxo oficial do pedido.
Depois que o cliente informar que ja enviou/concluiu o pedido, nao diga para acompanhar pelo site. Oriente que duvidas, ajustes e acompanhamento devem ser tratados por esta conversa ou pelo atendimento da unidade.
A edicao publica por link apos envio do pedido esta desativada. Nunca cite janela de 3 minutos, link de edicao, editar pelo site ou alterar pedido por link.
Nao feche pedido por conta propria e nao confirme pagamento automaticamente.
Toda conferencia de Pix e manual pela equipe da unidade. Nao analise comprovante, nao valide pagamento, nao solicite frase de confirmacao e nao diga que uma confirmacao foi registrada automaticamente.
Quando orientar Pix, deixe claro que a loja fara a conferencia manual antes da entrega.
Quando o cliente precisar concluir pedido, nome, endereco, CEP, numero da casa ou forma de pagamento, conduza para o fluxo oficial do ZeroPaper.
Link oficial da unidade para pedido: {company.AiAssistantOrderingLink ?? "NAO_CONFIGURADO"}
Se houver contexto seguro do WhatsApp com link personalizado do cliente, priorize esse link em vez do link generico.
Quando usar o link personalizado, diga que o site pode reaproveitar os dados do ultimo pedido daquele numero e que o cliente deve conferir ou alterar o endereco antes de enviar.
Se o cliente demonstrar intencao real de pedir, ver cardapio, fazer delivery ou comprar e o link estiver configurado, envie esse link de forma literal na resposta.
Nao use a frase "pelo link que te mandei" sem reenviar o link. Quando houver intencao real de pedir, envie o link literal novamente.
Se o cliente perguntar sobre Pix, responda com a chave configurada da unidade em mensagem curta e reforce que a conferencia e manual pela equipe. Se nao houver chave Pix configurada, diga que a unidade ainda nao informou essa chave no sistema.
Se o cliente informar que acabou de concluir o pedido no site, confirme com simpatia que a unidade recebeu o envio. Se ele precisar corrigir algo, oriente a falar nesta conversa para a equipe conferir com seguranca; nao prometa edicao automatica por link.
Mensagem inicial da unidade, para usar somente na primeira abordagem da conversa: {company.AiAssistantGreetingMessage}
Mensagem para redirecionar o cliente ao fluxo oficial: {company.AiAssistantRedirectMessage}
Mensagem de fallback da unidade: {company.AiAssistantFallbackMessage}
Essas mensagens podem estar em HTML simples e servem como base de linguagem da unidade.
Nao repita boas-vindas, saudacoes longas ou a apresentacao da unidade em toda resposta.
Depois da primeira mensagem, mantenha o atendimento corrido, educado e sem redundancia.
Ajude o cliente de verdade antes de redirecionar, mas preserve a regra de que pedido e pagamento acontecem pelo site.
Se a duvida do cliente for simples, responda com clareza e simpatia antes de qualquer encaminhamento.
Use a mensagem de fallback apenas quando realmente nao houver informacao suficiente ou quando responder direto puder gerar erro.
Se o cliente parecer indeciso, ajude como um atendente atencioso: explique com calma, deixe o caminho facil e convide para continuar sem pressionar.
Contexto e instrucoes da unidade:
{company.AiAssistantSystemPrompt}

Regras finais obrigatorias:
- Nao use "pelo link que te mandei" sem mandar o link junto.
- Se a pessoa demonstrar intencao de pedir, comprar, ver cardapio ou fazer delivery, envie o link oficial literal.
- Nao atue como cardapio no WhatsApp: nao liste itens, sabores, valores ou disponibilidade.
- Envie chave Pix na conversa somente se o cliente perguntar diretamente e houver chave configurada; mantenha a resposta curta e diga que a conferencia e manual.
- Toda conferencia de Pix e manual pela equipe. Nao valide comprovante, nao solicite frase de confirmacao e nao registre confirmacao automaticamente.
- Responda tempo de espera apenas quando o cliente perguntar. Use somente os tempos estimados configurados acima. Se nao estiver configurado, diga que a unidade ainda nao informou uma estimativa no sistema.
- Mantenha respostas simpaticas, curtas e uteis para reduzir custo de tokens.
""";
    }

    private static string BuildEstimatedWaitPrompt(Company company)
    {
        var lines = new List<string>();

        if (company.PickupEstimatedMinutes is > 0)
        {
            lines.Add($"retirada no local: {company.PickupEstimatedMinutes} minutos");
        }

        if (company.DeliveryEstimatedMinutes is > 0)
        {
            lines.Add($"entrega: {company.DeliveryEstimatedMinutes} minutos");
        }

        return lines.Count == 0
            ? "nenhum tempo estimado foi informado; se o cliente perguntar, diga que a unidade ainda nao informou uma estimativa no sistema."
            : string.Join("; ", lines) + ". Informe esses prazos somente se o cliente perguntar sobre tempo, demora, previsao ou espera.";
    }

    private static string BuildPixKeyPrompt(Company company)
    {
        if (string.IsNullOrWhiteSpace(company.AiAssistantPixKey))
        {
            return "NAO_CONFIGURADA";
        }

        var receiver = string.IsNullOrWhiteSpace(company.AiAssistantPixReceiverName)
            ? string.Empty
            : $" Recebedor: {company.AiAssistantPixReceiverName.Trim()}.";

        return $"{company.AiAssistantPixKey.Trim()}.{receiver} Informe somente se o cliente perguntar diretamente sobre Pix.";
    }

    private static bool TryBuildOutOfServiceReply(Company company, out string reply)
    {
        reply = string.Empty;

        var localNowForDay = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ServiceTimeZone);
        if (!IsServiceDayAllowed(company, localNowForDay.DayOfWeek))
        {
            var displayNameForDay = string.IsNullOrWhiteSpace(company.TradeName)
                ? company.LegalName
                : company.TradeName;
            var daysLabel = BuildServiceDaysLabel(ParseServiceDays(company.AiAssistantServiceDays));
            var windowNotice = TryResolveServiceWindow(company, out var configuredStart, out var configuredEnd)
                ? $"\n\nHorario nos dias de funcionamento: {configuredStart:HH\\:mm} as {configuredEnd:HH\\:mm}."
                : string.Empty;

            reply =
                $"Hoje {displayNameForDay} nao esta atendendo.\n\nA unidade nao recebe pedidos neste dia, entao o sistema de pedidos e pagamento pelo link fica fechado agora.\n\nDias de atendimento: {daysLabel}.{windowNotice}\n\nAssim que o atendimento reabrir, eu te ajudo por aqui.";
            return true;
        }

        if (!TryResolveServiceWindow(company, out var startTime, out var endTime))
        {
            return false;
        }

        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ServiceTimeZone);
        var currentLocalTime = TimeOnly.FromDateTime(currentLocalDateTime);
        var isWithinServiceWindow = IsWithinServiceWindow(currentLocalTime, startTime, endTime);

        if (isWithinServiceWindow)
        {
            return false;
        }

        var displayName = string.IsNullOrWhiteSpace(company.TradeName)
            ? company.LegalName
            : company.TradeName;
        var serviceWindow = $"{company.AiAssistantServiceStartTime} as {company.AiAssistantServiceEndTime}";
        var linkNotice = !string.IsNullOrWhiteSpace(company.AiAssistantOrderingLink)
            ? " Assim que o atendimento reabrir, voce pode continuar pelo link oficial da unidade 😊"
            : " Assim que o atendimento reabrir, a unidade volta a liberar pedidos por aqui 😊";

        reply =
            $"Agora {displayName} esta fora do horario de atendimento 🕒\n\nO sistema de pedidos fica fechado fora desse horario, entao nao e possivel pedir ou pagar pelo link neste momento.\n\nAtendimento: {serviceWindow}.{linkNotice}";
        return true;
    }

    private static bool TryResolveServiceWindow(Company company, out TimeOnly startTime, out TimeOnly endTime)
    {
        startTime = default;
        endTime = default;

        if (string.IsNullOrWhiteSpace(company.AiAssistantServiceStartTime) ||
            string.IsNullOrWhiteSpace(company.AiAssistantServiceEndTime))
        {
            return false;
        }

        return TimeOnly.TryParseExact(company.AiAssistantServiceStartTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out startTime) &&
               TimeOnly.TryParseExact(company.AiAssistantServiceEndTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out endTime);
    }

    private static string BuildServiceWindowPrompt(Company company)
    {
        var localNowForDay = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ServiceTimeZone);
        var daysLabel = BuildServiceDaysLabel(ParseServiceDays(company.AiAssistantServiceDays));
        if (!IsServiceDayAllowed(company, localNowForDay.DayOfWeek))
        {
            return $"Dias de atendimento configurados: {daysLabel}. Hoje e {BuildWeekDayName((int)localNowForDay.DayOfWeek)}. Status oficial agora: FECHADO hoje. A IA deve usar a resposta padrao de dia sem atendimento e nao deve conduzir para pedido, pagamento ou cobranca.";
        }

        if (string.IsNullOrWhiteSpace(company.AiAssistantServiceStartTime) ||
            string.IsNullOrWhiteSpace(company.AiAssistantServiceEndTime))
        {
            return $"Dias de atendimento configurados: {daysLabel}. Sem bloqueio de horario configurado no momento.";
        }

        if (!TryResolveServiceWindow(company, out var startTime, out var endTime))
        {
            return $"Dias de atendimento configurados: {daysLabel}. Sem bloqueio de horario configurado no momento. Status oficial agora: ABERTO para orientar pedidos.";
        }

        var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ServiceTimeZone);
        var currentLocalTime = TimeOnly.FromDateTime(currentLocalDateTime);
        var isOpen = IsWithinServiceWindow(currentLocalTime, startTime, endTime);
        var status = isOpen
            ? "ABERTO agora. A IA pode orientar o cliente e conduzir ao link oficial quando fizer sentido."
            : "FECHADO agora. A IA deve usar a resposta padrao de fora do horario e nao deve conduzir para pedido, pagamento ou cobranca.";

        return $"Dias de atendimento configurados: {daysLabel}. Atendimento configurado das {company.AiAssistantServiceStartTime} as {company.AiAssistantServiceEndTime}. Hora local atual: {currentLocalDateTime:HH:mm}. Status oficial agora: {status}";
    }

    private static bool TryResolveServiceDays(Company company, out IReadOnlyCollection<int> serviceDays)
    {
        var parsedDays = ParseServiceDays(company.AiAssistantServiceDays);
        serviceDays = parsedDays ?? [];
        return parsedDays is not null;
    }

    private static bool IsServiceDayAllowed(Company company, DayOfWeek dayOfWeek)
    {
        return !TryResolveServiceDays(company, out var serviceDays) ||
               serviceDays.Contains((int)dayOfWeek);
    }

    private static string BuildServiceDaysLabel(IReadOnlyCollection<int>? serviceDays)
    {
        if (serviceDays is null || serviceDays.Count == 0 || serviceDays.Count == 7)
        {
            return "todos os dias";
        }

        return string.Join(", ", OrderedServiceDays()
            .Where(item => serviceDays.Contains(item.Value))
            .Select(item => item.Label));
    }

    private static IEnumerable<(int Value, string Label)> OrderedServiceDays()
    {
        yield return (1, "segunda");
        yield return (2, "terca");
        yield return (3, "quarta");
        yield return (4, "quinta");
        yield return (5, "sexta");
        yield return (6, "sabado");
        yield return (0, "domingo");
    }

    private static string BuildWeekDayName(int day)
    {
        return OrderedServiceDays().FirstOrDefault(item => item.Value == day).Label ?? "hoje";
    }

    private static bool IsWithinServiceWindow(TimeOnly currentTime, TimeOnly startTime, TimeOnly endTime)
    {
        return startTime == endTime ||
               (startTime < endTime
                   ? currentTime >= startTime && currentTime <= endTime
                   : currentTime >= startTime || currentTime <= endTime);
    }

    private static TimeZoneInfo ResolveServiceTimeZone()
    {
        foreach (var timeZoneId in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    private string? BuildAbsoluteOrderingLink(string? pathOrUrl)
    {
        if (string.IsNullOrWhiteSpace(pathOrUrl))
        {
            return null;
        }

        if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var absoluteUri) &&
            absoluteUri.Scheme is "http" or "https")
        {
            return absoluteUri.ToString();
        }

        var baseUrl = ResolvePublicBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        var normalizedBaseUrl = baseUrl.TrimEnd('/');
        var normalizedPath = pathOrUrl.StartsWith('/') ? pathOrUrl : $"/{pathOrUrl}";
        return $"{normalizedBaseUrl}{normalizedPath}";
    }

    private string? ResolvePublicBaseUrl()
    {
        var configured = Environment.GetEnvironmentVariable("PUBLIC_APP_BASE_URL")
            ?? _publicAppOptions.BaseUrl;

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        return null;
    }

    private async Task RegisterInteractionAsync(
        Company company,
        string model,
        string source,
        bool succeeded,
        CancellationToken cancellationToken)
    {
        var interaction = new AiAssistantInteraction(
            company.TenantId,
            company.Id,
            source,
            model,
            succeeded);

        await _context.AiAssistantInteractions.AddAsync(interaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string? ResolveProtectedCredentialUpdate(string? currentCipherText, string? newValue, IDataProtector protector)
    {
        if (newValue is null)
        {
            return currentCipherText;
        }

        if (string.IsNullOrWhiteSpace(newValue))
        {
            return null;
        }

        return protector.Protect(newValue.Trim());
    }

    private static string CreateWebhookSecret()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(18)).ToLowerInvariant();
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
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Falha ao descriptografar um valor protegido da integracao de WhatsApp.");
            return null;
        }
    }

    private string? MaskProtectedValue(IDataProtector protector, string? cipherText)
    {
        var raw = UnprotectOrNull(protector, cipherText);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var visibleTail = raw.Length <= 6 ? raw : raw[^6..];
        return $"••••••{visibleTail}";
    }

    private WebhookUrls BuildWebhookUrls(string? instanceId, string? webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(webhookSecret))
        {
            return WebhookUrls.Empty;
        }

        var baseUrl = ResolvePublicBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return WebhookUrls.Empty;
        }

        var normalizedBaseUrl = baseUrl.TrimEnd('/');
        var escapedInstanceId = Uri.EscapeDataString(NormalizeEvolutionInstanceName(instanceId));
        var escapedKey = Uri.EscapeDataString(webhookSecret);

        return new WebhookUrls
        {
            ReceiveUrl = $"{normalizedBaseUrl}/api/integrations/whatsapp/evolution/{escapedInstanceId}/events?key={escapedKey}",
            MessageStatusUrl = null,
            ConnectedUrl = null,
            DisconnectedUrl = null
        };
    }

    private static string ExtractOutputText(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputTextElement) &&
            outputTextElement.ValueKind == JsonValueKind.String)
        {
            return outputTextElement.GetString() ?? string.Empty;
        }

        if (!root.TryGetProperty("output", out var outputElement) ||
            outputElement.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        foreach (var outputItem in outputElement.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var contentElement) ||
                contentElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in contentElement.EnumerateArray())
            {
                if (!contentItem.TryGetProperty("type", out var typeElement) ||
                    typeElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                if (!string.Equals(typeElement.GetString(), "output_text", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!contentItem.TryGetProperty("text", out var textElement) ||
                    textElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var segment = textElement.GetString();
                if (string.IsNullOrWhiteSpace(segment))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(segment.Trim());
            }
        }

        return builder.ToString();
    }

    private static string TruncateForLog(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : $"{normalized[..maxLength]}...";
    }

    private sealed class OpenAiResponsesRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public List<OpenAiResponsesMessage> Input { get; set; } = [];

        [JsonPropertyName("max_output_tokens")]
        public int MaxOutputTokens { get; set; }
    }

    private sealed class OpenAiResponsesMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public List<OpenAiResponsesContent> Content { get; set; } = [];
    }

    private sealed class OpenAiResponsesContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class GeneratedTemplate
    {
        public string SystemPrompt { get; set; } = string.Empty;
        public string GreetingMessage { get; set; } = string.Empty;
        public string RedirectMessage { get; set; } = string.Empty;
        public string FallbackMessage { get; set; } = string.Empty;
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

    private sealed class WebhookUrls
    {
        public static readonly WebhookUrls Empty = new();

        public string? ReceiveUrl { get; set; }
        public string? MessageStatusUrl { get; set; }
        public string? ConnectedUrl { get; set; }
        public string? DisconnectedUrl { get; set; }
    }
}
