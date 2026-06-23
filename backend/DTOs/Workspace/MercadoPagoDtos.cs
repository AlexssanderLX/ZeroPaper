namespace ZeroPaper.DTOs.Workspace;

public class MercadoPagoStatusDto
{
    public bool Configured { get; init; }
    public bool Connected { get; init; }
    public string? AccountUserId { get; init; }
    public bool LiveMode { get; init; }
    public DateTime? ConnectedAtUtc { get; init; }
}

public class MercadoPagoConnectResponseDto
{
    public string AuthorizationUrl { get; init; } = string.Empty;
}

public class MercadoPagoCheckoutResponseDto
{
    public bool Available { get; init; }
    public string? InitPoint { get; init; }
    public string? PreferenceId { get; init; }
    public string Message { get; init; } = string.Empty;
}
