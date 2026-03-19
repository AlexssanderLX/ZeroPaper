using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class Company : TenantOwnedEntity
{
    private readonly List<AppUser> _users = [];
    private readonly List<AppSession> _sessions = [];
    private readonly List<QrCodeAccess> _qrCodeAccesses = [];
    private readonly List<DiningTable> _tables = [];
    private readonly List<CustomerOrder> _orders = [];
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

    public Tenant Tenant { get; private set; } = null!;
    public IReadOnlyCollection<AppUser> Users => _users.AsReadOnly();
    public IReadOnlyCollection<AppSession> Sessions => _sessions.AsReadOnly();
    public IReadOnlyCollection<QrCodeAccess> QrCodeAccesses => _qrCodeAccesses.AsReadOnly();
    public IReadOnlyCollection<DiningTable> Tables => _tables.AsReadOnly();
    public IReadOnlyCollection<CustomerOrder> Orders => _orders.AsReadOnly();
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
}
