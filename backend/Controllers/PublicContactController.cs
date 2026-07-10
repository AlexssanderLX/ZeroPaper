using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using ZeroPaper.DTOs.Public;

namespace ZeroPaper.Controllers;

[ApiController]
[Route("api/public/contact")]
public class PublicContactController : ControllerBase
{
    private const string Recipient = "alexssander.f.almeida2006@gmail.com";
    private readonly IConfiguration _configuration;
    private readonly ILogger<PublicContactController> _logger;

    public PublicContactController(IConfiguration configuration, ILogger<PublicContactController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ContactMessageResponseDto>> Send(
        [FromBody] ContactMessageDto dto,
        CancellationToken cancellationToken)
    {
        var host = _configuration["Email:Smtp:Host"];
        var port = _configuration.GetValue<int?>("Email:Smtp:Port");
        var username = _configuration["Email:Smtp:Username"];
        var password = _configuration["Email:Smtp:Password"];
        var senderEmail = _configuration["Email:Smtp:SenderEmail"] ?? username;
        var senderName = _configuration["Email:Smtp:SenderName"] ?? "ZeroPaper";
        var useSsl = _configuration.GetValue("Email:Smtp:UseSsl", true);

        if (string.IsNullOrWhiteSpace(host) || !port.HasValue ||
            string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Contact form: SMTP not configured");
            return StatusCode(503, new ContactMessageResponseDto { Sent = false, Info = "Servico de email indisponivel." });
        }

        var body = new StringBuilder();
        body.AppendLine("Nova mensagem de contato — ZeroPaper");
        body.AppendLine();
        body.AppendLine($"Email: {dto.Email.Trim().ToLowerInvariant()}");
        body.AppendLine($"Telefone: {(string.IsNullOrWhiteSpace(dto.Phone) ? "Nao informado" : dto.Phone.Trim())}");
        body.AppendLine();
        body.AppendLine("Mensagem:");
        body.AppendLine(dto.Message.Trim());
        body.AppendLine();
        body.AppendLine($"Enviado em UTC: {DateTime.UtcNow:O}");

        using var message = new MailMessage
        {
            From = new MailAddress(senderEmail!, senderName),
            Subject = $"Contato via site — {dto.Email.Trim()}",
            Body = body.ToString(),
            IsBodyHtml = false,
        };

        message.To.Add(Recipient);
        message.ReplyToList.Add(new MailAddress(dto.Email.Trim().ToLowerInvariant()));

        using var client = new SmtpClient(host, port.Value)
        {
            EnableSsl = useSsl,
            Credentials = new NetworkCredential(username, password),
        };

        using var reg = cancellationToken.Register(client.SendAsyncCancel);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Contact form: failed to send email from {Email}", dto.Email);
            return StatusCode(500, new ContactMessageResponseDto { Sent = false, Info = "Falha ao enviar mensagem. Tente novamente." });
        }

        return Ok(new ContactMessageResponseDto
        {
            Sent = true,
            Info = "Mensagem enviada! Responderei em breve.",
        });
    }
}
