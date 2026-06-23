namespace ZeroPaper.Services.Models;

public class MercadoPagoOptions
{
    public const string SectionName = "MercadoPago";

    public const string DefaultApiBaseUrl = "https://api.mercadopago.com/";
    public const string DefaultAuthBaseUrl = "https://auth.mercadopago.com.br/";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? ApiBaseUrl { get; set; }
    public string? AuthBaseUrl { get; set; }
}
