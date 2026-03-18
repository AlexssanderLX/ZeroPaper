using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class Tenant : BaseEntity
{
    private readonly List<Company> _companies = [];
    private readonly List<AppUser> _users = [];
    private readonly List<AppSession> _sessions = [];
    private readonly List<Subscription> _subscriptions = [];
    private readonly List<QrCodeAccess> _qrCodeAccesses = [];
    private readonly List<DiningTable> _tables = [];
    private readonly List<CustomerOrder> _orders = [];
    private readonly List<StockItem> _stockItems = [];

    private Tenant()
    {
    }

    public Tenant(string name, string identifier)
    {
        Rename(name);
        ChangeIdentifier(identifier);
    }

    public string Name { get; private set; } = null!;
    public string Identifier { get; private set; } = null!;

    public IReadOnlyCollection<Company> Companies => _companies.AsReadOnly();
    public IReadOnlyCollection<AppUser> Users => _users.AsReadOnly();
    public IReadOnlyCollection<AppSession> Sessions => _sessions.AsReadOnly();
    public IReadOnlyCollection<Subscription> Subscriptions => _subscriptions.AsReadOnly();
    public IReadOnlyCollection<QrCodeAccess> QrCodeAccesses => _qrCodeAccesses.AsReadOnly();
    public IReadOnlyCollection<DiningTable> Tables => _tables.AsReadOnly();
    public IReadOnlyCollection<CustomerOrder> Orders => _orders.AsReadOnly();
    public IReadOnlyCollection<StockItem> StockItems => _stockItems.AsReadOnly();

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Touch();
    }

    public void ChangeIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        Identifier = identifier.Trim().ToLowerInvariant();
        Touch();
    }
}
