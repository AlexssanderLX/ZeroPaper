namespace ZeroPaper.Services.Models;

public class WorkspaceSessionContext
{
    public Guid TenantId { get; init; }
    public Guid CompanyId { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string RestaurantName { get; init; } = string.Empty;
}

