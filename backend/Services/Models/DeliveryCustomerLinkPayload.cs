namespace ZeroPaper.Services.Models;

public class DeliveryCustomerLinkPayload
{
    public Guid CompanyId { get; set; }
    public string PublicCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; }
}
