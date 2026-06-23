using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public class PrintJob : TenantOwnedEntity
{
    private PrintJob()
    {
    }

    private PrintJob(Guid tenantId, Guid companyId, PrintJobKind kind, string title, string? notes) : base(tenantId)
    {
        CompanyId = companyId;
        Kind = kind;
        Status = PrintStatus.Pending;
        QueuedAtUtc = DateTime.UtcNow;
        Title = NormalizeRequired(title, 160, nameof(title));
        Notes = Normalize(notes, 600);
    }

    public Guid CompanyId { get; private set; }
    public Guid? SourceOrderId { get; private set; }
    public PrintJobKind Kind { get; private set; }
    public PrintStatus Status { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Notes { get; private set; }
    public string? AgentName { get; private set; }
    public string? PrinterName { get; private set; }
    public string? LastError { get; private set; }
    public DateTime QueuedAtUtc { get; private set; }
    public DateTime? ClaimedAtUtc { get; private set; }
    public DateTime? PrintedAtUtc { get; private set; }
    public int Attempts { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public Company Company { get; private set; } = null!;

    public static PrintJob CreateTest(Guid tenantId, Guid companyId, string? notes)
    {
        return new PrintJob(tenantId, companyId, PrintJobKind.Test, "Teste de impressao ZeroPaper", notes);
    }

    public void Claim(string agentName, string? printerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        Status = PrintStatus.Processing;
        ClaimedAtUtc = DateTime.UtcNow;
        PrintedAtUtc = null;
        Attempts += 1;
        LastError = null;
        AgentName = NormalizeRequired(agentName, 120, nameof(agentName));
        PrinterName = Normalize(printerName, 180);
        Touch();
    }

    public void MarkPrinted(string agentName, string? printerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        Status = PrintStatus.Printed;
        PrintedAtUtc = DateTime.UtcNow;
        LastError = null;
        AgentName = NormalizeRequired(agentName, 120, nameof(agentName));
        PrinterName = Normalize(printerName, 180);
        Touch();
    }

    public void MarkFailed(string? errorMessage, string agentName, string? printerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        Status = PrintStatus.Failed;
        LastError = string.IsNullOrWhiteSpace(errorMessage)
            ? "Falha desconhecida ao imprimir."
            : Normalize(errorMessage, 500);
        AgentName = NormalizeRequired(agentName, 120, nameof(agentName));
        PrinterName = Normalize(printerName, 180);
        Touch();
    }

    private static string NormalizeRequired(string value, int maxLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
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
