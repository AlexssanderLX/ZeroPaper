using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using ZeroPaper.Data;
using ZeroPaper.Domain.Entities;
using ZeroPaper.DTOs.Auth;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Services;

public class PasswordResetService : IPasswordResetService
{
    private static readonly TimeSpan ResetLifetime = TimeSpan.FromMinutes(30);
    private const string GenericMessage = "Se o email estiver cadastrado, voce vai receber um link para redefinir a senha.";
    private readonly ZeroPaperDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher _passwordHasher;

    public PasswordResetService(
        ZeroPaperDbContext context,
        IConfiguration configuration,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
    }

    public async Task<PasswordResetRequestResponseDto> RequestAsync(PasswordResetRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return BuildGenericResponse();
        }

        var user = await _context.Users
            .Include(item => item.Company)
            .FirstOrDefaultAsync(
                item => item.Email == normalizedEmail && item.IsActive && item.Company.IsActive,
                cancellationToken);

        if (user is null)
        {
            return BuildGenericResponse();
        }

        var utcNow = DateTime.UtcNow;
        var activeRequests = await _context.PasswordResetRequests
            .Where(item => item.AppUserId == user.Id && item.IsActive && item.UsedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var activeRequest in activeRequests)
        {
            activeRequest.Revoke();
        }

        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var resetRequest = new PasswordResetRequest(
            user.Id,
            ComputeHash(rawToken),
            utcNow.Add(ResetLifetime));

        await _context.PasswordResetRequests.AddAsync(resetRequest, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await SendResetEmailAsync(user, rawToken, cancellationToken);

        return BuildGenericResponse();
    }

    public async Task<bool> ResetAsync(ResetPasswordDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return false;
        }

        var utcNow = DateTime.UtcNow;
        var tokenHash = ComputeHash(request.Token);

        var resetRequest = await _context.PasswordResetRequests
            .Include(item => item.AppUser)
            .FirstOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (resetRequest is null || !resetRequest.IsAvailable(utcNow) || !resetRequest.AppUser.IsActive)
        {
            return false;
        }

        if (request.NewPassword.Trim().Length < 8)
        {
            return false;
        }

        resetRequest.AppUser.ChangePasswordHash(_passwordHasher.Hash(request.NewPassword));
        resetRequest.MarkAsUsed(utcNow);

        var sessions = await _context.Sessions
            .Where(item => item.AppUserId == resetRequest.AppUserId && item.IsActive && item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke(utcNow);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task SendResetEmailAsync(AppUser user, string rawToken, CancellationToken cancellationToken)
    {
        var host = _configuration["Email:Smtp:Host"];
        var port = _configuration.GetValue<int?>("Email:Smtp:Port");
        var username = _configuration["Email:Smtp:Username"];
        var password = _configuration["Email:Smtp:Password"];
        var senderEmail = _configuration["Email:Smtp:SenderEmail"] ?? username;
        var senderName = _configuration["Email:Smtp:SenderName"] ?? "ZeroPaper";
        var useSsl = _configuration.GetValue("Email:Smtp:UseSsl", true);

        if (string.IsNullOrWhiteSpace(host) ||
            !port.HasValue ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(senderEmail))
        {
            throw new InvalidOperationException("Email delivery is not configured.");
        }

        var frontendBaseUrl = _configuration["Frontend:PublicBaseUrl"]
            ?? _configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>()?.FirstOrDefault()
            ?? "http://localhost:3000";

        var resetUrl = $"{frontendBaseUrl.TrimEnd('/')}/redefinir-senha?token={Uri.EscapeDataString(rawToken)}";
        var restaurantName = user.Role.ToString().Equals("Root", StringComparison.OrdinalIgnoreCase)
            ? "ZeroPaper"
            : user.Company.TradeName;

        using var message = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = $"Redefinicao de senha - {restaurantName}",
            Body = BuildResetBody(user, restaurantName, resetUrl),
            IsBodyHtml = false
        };

        message.To.Add(user.Email);

        using var client = new SmtpClient(host, port.Value)
        {
            EnableSsl = useSsl,
            Credentials = new NetworkCredential(username, password)
        };

        using var registration = cancellationToken.Register(client.SendAsyncCancel);
        await client.SendMailAsync(message, cancellationToken);
    }

    private static string BuildResetBody(AppUser user, string restaurantName, string resetUrl)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Ola, {user.FullName}.");
        builder.AppendLine();
        builder.AppendLine($"Recebemos um pedido para redefinir a senha do seu acesso na {restaurantName}.");
        builder.AppendLine("Se foi voce, use o link abaixo para criar uma nova senha:");
        builder.AppendLine(resetUrl);
        builder.AppendLine();
        builder.AppendLine("Esse link expira em 30 minutos e invalida os acessos anteriores depois da troca de senha.");
        builder.AppendLine("Se voce nao fez esse pedido, pode ignorar este email.");
        return builder.ToString();
    }

    private static PasswordResetRequestResponseDto BuildGenericResponse()
    {
        return new PasswordResetRequestResponseDto
        {
            Accepted = true,
            Message = GenericMessage
        };
    }

    private static string ComputeHash(string rawValue)
    {
        var bytes = Encoding.UTF8.GetBytes(rawValue.Trim());
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }
}
