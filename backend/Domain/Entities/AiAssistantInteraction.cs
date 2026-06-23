using ZeroPaper.Domain.Common;

namespace ZeroPaper.Domain.Entities;

public class AiAssistantInteraction : TenantOwnedEntity
{
    private AiAssistantInteraction()
    {
    }

    public AiAssistantInteraction(
        Guid tenantId,
        Guid companyId,
        string source,
        string model,
        bool succeeded) : base(tenantId)
    {
        CompanyId = companyId;
        UpdateSource(source);
        UpdateModel(model);
        Succeeded = succeeded;
    }

    public Guid CompanyId { get; private set; }
    public string Source { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public bool Succeeded { get; private set; }

    public Company Company { get; private set; } = null!;

    private void UpdateSource(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        Source = source.Trim();
        Touch();
    }

    private void UpdateModel(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        Model = model.Trim();
        Touch();
    }
}
