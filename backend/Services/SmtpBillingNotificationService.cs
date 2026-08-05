using System.Net;
using System.Net.Mail;
using ZeroPaper.Domain.Entities;
using ZeroPaper.Services.Interfaces;

namespace ZeroPaper.Services;

public sealed class SmtpBillingNotificationService(IConfiguration configuration, ILogger<SmtpBillingNotificationService> logger) : IBillingNotificationService
{
    public async Task SendPaymentConfirmedAsync(Company company, AppUser owner, Subscription subscription, SubscriptionPayment payment, CancellationToken cancellationToken = default)
    {
        var host = configuration["Email:Smtp:Host"];
        var port = configuration.GetValue<int?>("Email:Smtp:Port");
        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        var sender = configuration["Email:Smtp:SenderEmail"] ?? username;
        var recipient = configuration["Email:AccessRequests:Recipient"] ?? "alexssander.f.almeida2006@gmail.com";
        if (string.IsNullOrWhiteSpace(host) || !port.HasValue || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(sender))
        {
            logger.LogWarning("Billing payment email skipped because SMTP is not configured. Payment {PaymentId}", payment.ExternalPaymentId);
            return;
        }
        using var message = new MailMessage(new MailAddress(sender, "ZeroPaper"), new MailAddress(recipient))
        {
            Subject = $"Mensalidade confirmada - {company.TradeName}",
            Body = $"Pagamento confirmado\n\nEmpresa: {company.TradeName}\nOwner: {owner.FullName} ({owner.Email})\nPlano: {subscription.PlanName}\nValor: R$ {payment.Amount:N2}\nOrigem: {payment.Source}\nPago em UTC: {payment.PaidAtUtc:O}\nAcesso valido ate UTC: {subscription.PaidThroughUtc:O}",
            IsBodyHtml = false
        };
        using var client = new SmtpClient(host, port.Value) { EnableSsl = configuration.GetValue("Email:Smtp:UseSsl", true), Credentials = new NetworkCredential(username, password) };
        await client.SendMailAsync(message, cancellationToken);
    }
}
