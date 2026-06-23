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
    public string PlanName { get; init; } = string.Empty;
    public string PlanTier { get; init; } = string.Empty;
    public bool IncludesMenuModule { get; init; } = true;
    public bool IncludesTablesModule { get; init; } = true;
    public bool IncludesKitchenModule { get; init; } = true;
    public bool IncludesCashModule { get; init; } = true;
    public bool IncludesStockModule { get; init; } = true;
    public bool IncludesDeliveryModule { get; init; } = true;
    public bool IncludesPrintingModule { get; init; } = true;
    public bool IncludesWaiterCallModule { get; init; } = true;
    public bool IncludesAiAssistantModule { get; init; }
    public bool HasWhatsAppAI { get; init; }
    public bool HasDelivery { get; init; }
    public bool HasAutoPrint { get; init; }
    public bool HasBasicReports { get; init; }
    public bool HasManagementDashboard { get; init; }
    public bool HasAdvancedReports { get; init; }
    public bool HasCoupons { get; init; }
    public bool HasRecurringCustomers { get; init; }
}
