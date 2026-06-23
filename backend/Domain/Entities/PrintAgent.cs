using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class PrintAgent : TenantOwnedEntity
{
    private PrintAgent()
    {
    }

    public PrintAgent(Guid tenantId, Guid companyId, string tokenHash) : base(tenantId)
    {
        CompanyId = companyId;
        RotateToken(tokenHash);
    }

    public Guid CompanyId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public string? Name { get; private set; }
    public string? PrinterName { get; private set; }
    public string? AppVersion { get; private set; }
    public DateTime? RegisteredAtUtc { get; private set; }
    public DateTime? LastSeenAtUtc { get; private set; }
    public DateTime TokenRotatedAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? LastErrorAtUtc { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;

    public void RotateToken(string tokenHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        TokenHash = tokenHash.Trim();
        TokenRotatedAtUtc = DateTime.UtcNow;
        RegisteredAtUtc = null;
        LastSeenAtUtc = null;
        LastError = null;
        LastErrorAtUtc = null;
        Touch();
    }

    public void Register(string agentName, string? printerName, string? appVersion, DateTime utcNow)
    {
        UpdateHeartbeat(agentName, printerName, appVersion, utcNow);
        RegisteredAtUtc ??= utcNow;
    }

    public void UpdateHeartbeat(string agentName, string? printerName, string? appVersion, DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        Name = Normalize(agentName, 120);
        PrinterName = Normalize(printerName, 180);
        AppVersion = Normalize(appVersion, 60);
        LastSeenAtUtc = utcNow;
        Touch();
    }

    public void ClearError()
    {
        LastError = null;
        LastErrorAtUtc = null;
        Touch();
    }

    public void RegisterError(string? errorMessage, DateTime utcNow)
    {
        LastError = string.IsNullOrWhiteSpace(errorMessage)
            ? "Falha desconhecida ao imprimir."
            : Normalize(errorMessage, 500);
        LastErrorAtUtc = utcNow;
        Touch();
    }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
