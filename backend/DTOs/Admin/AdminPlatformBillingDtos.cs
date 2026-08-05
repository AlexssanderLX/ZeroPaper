namespace ZeroPaper.DTOs.Admin;

public sealed class AdminPlatformBillingStatusDto
{
    public bool Configured { get; set; }
    public string Provider { get; set; } = "MercadoPago";
    public string? AccountUserId { get; set; }
    public string? AccountEmail { get; set; }
    public bool LiveMode { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class ConfigureAdminPlatformBillingRequestDto : AdminSensitiveActionRequestDto
{
    public string AccessToken { get; set; } = string.Empty;
}

public sealed class AdminSubscriptionCheckoutDto
{
    public Guid CompanyId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public string? MercadoPagoStatus { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTime? StatusUpdatedAtUtc { get; set; }
}
