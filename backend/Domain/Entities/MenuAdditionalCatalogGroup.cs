using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class MenuAdditionalCatalogGroup : TenantOwnedEntity
{
    private readonly List<MenuAdditionalCatalogOption> _options = [];

    private MenuAdditionalCatalogGroup()
    {
    }

    public MenuAdditionalCatalogGroup(
        Guid tenantId,
        Guid companyId,
        string name,
        bool allowMultiple,
        int displayOrder = 0,
        int? maxAdditionalSelections = null,
        IEnumerable<MenuAdditionalCatalogOption>? options = null) : base(tenantId)
    {
        CompanyId = companyId;
        Rename(name);
        SetAllowMultiple(allowMultiple);
        SetDisplayOrder(displayOrder);
        SetMaxAdditionalSelections(maxAdditionalSelections);

        if (options is not null)
        {
            ReplaceOptions(options);
        }
    }

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public bool AllowMultiple { get; private set; }
    public int DisplayOrder { get; private set; }
    public int? MaxAdditionalSelections { get; private set; }

    public IReadOnlyCollection<MenuAdditionalCatalogOption> Options => _options.AsReadOnly();

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Touch();
    }

    public void SetAllowMultiple(bool allowMultiple)
    {
        AllowMultiple = allowMultiple;
        Touch();
    }

    public void SetDisplayOrder(int displayOrder)
    {
        DisplayOrder = Math.Max(0, displayOrder);
        Touch();
    }

    public void SetMaxAdditionalSelections(int? maxAdditionalSelections)
    {
        if (maxAdditionalSelections is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAdditionalSelections), "O limite de adicionais nao pode ser negativo.");
        }

        if (maxAdditionalSelections is > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAdditionalSelections), "O limite de adicionais precisa ser menor que 100.");
        }

        MaxAdditionalSelections = maxAdditionalSelections;
        Touch();
    }

    public void ReplaceOptions(IEnumerable<MenuAdditionalCatalogOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options.Clear();
        _options.AddRange(options);
        Touch();
    }
}
