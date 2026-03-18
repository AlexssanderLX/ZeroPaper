using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class Tenant : BaseEntity
{
    private readonly List<Company> _companies = [];
    private readonly List<AppUser> _users = [];
    private readonly List<Subscription> _subscriptions = [];
    private readonly List<QrCodeAccess> _qrCodeAccesses = [];

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
    public IReadOnlyCollection<Subscription> Subscriptions => _subscriptions.AsReadOnly();
    public IReadOnlyCollection<QrCodeAccess> QrCodeAccesses => _qrCodeAccesses.AsReadOnly();

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
