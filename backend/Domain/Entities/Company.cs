using ZeroPaper.Domain.Common;
using ZeroPaper.Domain.Enums;

namespace ZeroPaper.Domain.Entities;

public class Company : TenantOwnedEntity
{
    private readonly List<AppUser> _users = [];
    private readonly List<AppSession> _sessions = [];
    private readonly List<QrCodeAccess> _qrCodeAccesses = [];
    private readonly List<DiningTable> _tables = [];
    private readonly List<CustomerOrder> _orders = [];
    private readonly List<WaiterCall> _waiterCalls = [];
    private readonly List<StockItem> _stockItems = [];
    private readonly List<MenuCategory> _menuCategories = [];
    private readonly List<MenuItem> _menuItems = [];

    private Company()
    {
    }

    public Company(
        Guid tenantId,
        string legalName,
        string tradeName,
        string accessSlug,
        string? documentNumber = null,
        string? contactEmail = null,
        string? contactPhone = null) : base(tenantId)
    {
        UpdateNames(legalName, tradeName);
        ChangeAccessSlug(accessSlug);
        UpdateContact(contactEmail, contactPhone);
        DocumentNumber = documentNumber?.Trim();
    }

    public string LegalName { get; private set; } = null!;
    public string TradeName { get; private set; } = null!;
    public string AccessSlug { get; private set; } = null!;
    public string? DocumentNumber { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public int LastOrderNumber { get; private set; }
    public bool EnableOrderAlerts { get; private set; } = true;
    public bool EnableWaiterCallAlerts { get; private set; } = true;
    public string? AlertSoundUrl { get; private set; }
    public int AlertVolumePercent { get; private set; } = 100;
    public int AlertPlaybackSeconds { get; private set; } = 6;
    public bool EnableAutomaticPrinting { get; private set; } = true;
    public PrintPaperProfile PrintPaperProfile { get; private set; } = PrintPaperProfile.Thermal80mm;
    public int PrintOrdersPerPage { get; private set; } = 1;
    public string? PrintAgentKeyHash { get; private set; }
    public string? PrintAgentName { get; private set; }
    public string? PrintAgentPrinterName { get; private set; }
    public DateTime? PrintAgentLastSeenAtUtc { get; private set; }

    public Tenant Tenant { get; private set; } = null!;
    public IReadOnlyCollection<AppUser> Users => _users.AsReadOnly();
    public IReadOnlyCollection<AppSession> Sessions => _sessions.AsReadOnly();
    public IReadOnlyCollection<QrCodeAccess> QrCodeAccesses => _qrCodeAccesses.AsReadOnly();
    public IReadOnlyCollection<DiningTable> Tables => _tables.AsReadOnly();
    public IReadOnlyCollection<CustomerOrder> Orders => _orders.AsReadOnly();
    public IReadOnlyCollection<WaiterCall> WaiterCalls => _waiterCalls.AsReadOnly();
    public IReadOnlyCollection<StockItem> StockItems => _stockItems.AsReadOnly();
    public IReadOnlyCollection<MenuCategory> MenuCategories => _menuCategories.AsReadOnly();
    public IReadOnlyCollection<MenuItem> MenuItems => _menuItems.AsReadOnly();

    public void UpdateNames(string legalName, string tradeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tradeName);

        LegalName = legalName.Trim();
        TradeName = tradeName.Trim();
        Touch();
    }

    public void ChangeAccessSlug(string accessSlug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessSlug);
        AccessSlug = accessSlug.Trim().ToLowerInvariant();
        Touch();
    }

    public void UpdateContact(string? contactEmail, string? contactPhone)
    {
        ContactEmail = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail.Trim().ToLowerInvariant();
        ContactPhone = string.IsNullOrWhiteSpace(contactPhone) ? null : contactPhone.Trim();
        Touch();
    }

    public int ReserveNextOrderNumber()
    {
        LastOrderNumber += 1;
        Touch();
        return LastOrderNumber;
    }

    public void UpdateAlertPreferences(bool enableOrderAlerts, bool enableWaiterCallAlerts, int alertVolumePercent, int alertPlaybackSeconds)
    {
        if (alertVolumePercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(alertVolumePercent), "O volume precisa ficar entre 0 e 100.");
        }

        if (alertPlaybackSeconds is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(alertPlaybackSeconds), "A duracao precisa ficar entre 1 e 20 segundos.");
        }

        EnableOrderAlerts = enableOrderAlerts;
        EnableWaiterCallAlerts = enableWaiterCallAlerts;
        AlertVolumePercent = alertVolumePercent;
        AlertPlaybackSeconds = alertPlaybackSeconds;
        Touch();
    }

    public void UpdateAlertSound(string? alertSoundUrl)
    {
        AlertSoundUrl = string.IsNullOrWhiteSpace(alertSoundUrl) ? null : alertSoundUrl.Trim();
        Touch();
    }

    public void UpdatePrintingPreferences(bool enableAutomaticPrinting, PrintPaperProfile printPaperProfile, int printOrdersPerPage)
    {
        EnableAutomaticPrinting = enableAutomaticPrinting;
        PrintPaperProfile = printPaperProfile;
        PrintOrdersPerPage = NormalizePrintOrdersPerPage(printPaperProfile, printOrdersPerPage);
        Touch();
    }

    public void RotatePrintAgentKey(string keyHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHash);

        PrintAgentKeyHash = keyHash.Trim();
        PrintAgentName = null;
        PrintAgentPrinterName = null;
        PrintAgentLastSeenAtUtc = null;
        Touch();
    }

    public void RegisterPrintAgentHeartbeat(string agentName, string? printerName, DateTime seenAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        PrintAgentName = agentName.Trim();
        PrintAgentPrinterName = string.IsNullOrWhiteSpace(printerName) ? null : printerName.Trim();
        PrintAgentLastSeenAtUtc = seenAtUtc;
        Touch();
    }

    private static int NormalizePrintOrdersPerPage(PrintPaperProfile printPaperProfile, int printOrdersPerPage)
    {
        if (printPaperProfile == PrintPaperProfile.Thermal80mm)
        {
            return 1;
        }

        return printOrdersPerPage is 2 or 4 ? printOrdersPerPage : 1;
    }
}
