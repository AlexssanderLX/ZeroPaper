using System.Net;
using System.Net.Mail;
using System.Text;
using ZeroPaper.DTOs.Public;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Services;

public class SmtpAccessRequestNotificationService : IAccessRequestNotificationService
{
    private const string DefaultRecipient = "alexssander.f.almeida2006@gmail.com";
    private readonly IConfiguration _configuration;

    public SmtpAccessRequestNotificationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<AccessRequestResponseDto> SendAsync(AccessRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var host = _configuration["Email:Smtp:Host"];
        var port = _configuration.GetValue<int?>("Email:Smtp:Port");
        var username = _configuration["Email:Smtp:Username"];
        var password = _configuration["Email:Smtp:Password"];
        var senderEmail = _configuration["Email:Smtp:SenderEmail"] ?? username;
        var senderName = _configuration["Email:Smtp:SenderName"] ?? "ZeroPaper";
        var recipient = _configuration["Email:AccessRequests:Recipient"] ?? DefaultRecipient;
        var useSsl = _configuration.GetValue("Email:Smtp:UseSsl", true);

        if (string.IsNullOrWhiteSpace(host) ||
            !port.HasValue ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(senderEmail))
        {
            throw new InvalidOperationException("Email delivery is not configured.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = $"Nova solicitacao de liberacao ZeroPaper - {request.RestaurantName.Trim()}",
            Body = BuildBody(request),
            IsBodyHtml = false
        };

        message.To.Add(recipient);
        message.ReplyToList.Add(new MailAddress(request.OwnerEmail.Trim().ToLowerInvariant(), request.OwnerName.Trim()));

        using var client = new SmtpClient(host, port.Value)
        {
            EnableSsl = useSsl,
            Credentials = new NetworkCredential(username, password)
        };

        using var registration = cancellationToken.Register(client.SendAsyncCancel);
        await client.SendMailAsync(message, cancellationToken);

        return new AccessRequestResponseDto
        {
            Accepted = true,
            Message = "Solicitacao enviada com sucesso. Em breve voce recebe um retorno da ZeroPaper."
        };
    }

    private static string BuildBody(AccessRequestDto request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Nova solicitacao de liberacao ZeroPaper");
        builder.AppendLine();
        builder.AppendLine($"Restaurante: {request.RestaurantName.Trim()}");
        builder.AppendLine($"Razao social: {NormalizeOptional(request.LegalName)}");
        builder.AppendLine($"Responsavel: {request.OwnerName.Trim()}");
        builder.AppendLine($"Email: {request.OwnerEmail.Trim().ToLowerInvariant()}");
        builder.AppendLine($"Telefone: {NormalizeOptional(request.ContactPhone)}");
        builder.AppendLine($"Cidade/Bairro: {NormalizeOptional(request.CityRegion)}");
        builder.AppendLine($"Observacoes: {NormalizeOptional(request.Notes)}");
        builder.AppendLine();
        builder.AppendLine($"Enviado em UTC: {DateTime.UtcNow:O}");
        return builder.ToString();
    }

    private static string NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Nao informado" : value.Trim();
    }
}
