namespace ZeroPaper.DTOs.Admin;

public class AdminSensitiveActionRequestDto
{
    public string Password { get; set; } = string.Empty;
}

public class UpdateAdminCompanyPlanRequestDto : AdminSensitiveActionRequestDto
{
    public string? PlanName { get; set; }
    public bool IncludesMenuModule { get; set; }
    public bool IncludesTablesModule { get; set; }
    public bool IncludesKitchenModule { get; set; }
    public bool IncludesCashModule { get; set; }
    public bool IncludesStockModule { get; set; }
    public bool IncludesDeliveryModule { get; set; }
    public bool IncludesPrintingModule { get; set; }
    public bool IncludesWaiterCallModule { get; set; }
    public bool IncludesAiAssistantModule { get; set; }
    public int MaxUsers { get; set; }
}
