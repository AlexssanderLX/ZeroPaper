namespace ZeroPaper.DTOs.Admin;

public class AdminDashboardDto
{
    public AdminDashboardSummaryDto Summary { get; set; } = new();
    public List<SignupCodeDto> Codes { get; set; } = [];
    public List<AdminUserDto> Users { get; set; } = [];
    public List<AdminCompanyFlowDto> Companies { get; set; } = [];
}

public class AdminDashboardSummaryDto
{
    public int TotalCompanies { get; set; }
    public int ActiveCompanies { get; set; }
    public int TotalUsers { get; set; }
    public int OnlineUsers { get; set; }
    public int AvailableSignupCodes { get; set; }
    public int UsedSignupCodes { get; set; }
    public int ExpiredSignupCodes { get; set; }
    public int OrdersToday { get; set; }
    public int OpenOrders { get; set; }
    public int PendingPayments { get; set; }
    public int FailedPrints { get; set; }
    public int AiInteractionsToday { get; set; }
}

public class AdminCompanyFlowDto
{
    public Guid CompanyId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string AccessSlug { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanTier { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int MaxUsers { get; set; }
    public bool IncludesMenuModule { get; set; }
    public bool IncludesTablesModule { get; set; }
    public bool IncludesKitchenModule { get; set; }
    public bool IncludesCashModule { get; set; }
    public bool IncludesStockModule { get; set; }
    public bool IncludesDeliveryModule { get; set; }
    public bool IncludesPrintingModule { get; set; }
    public bool IncludesWaiterCallModule { get; set; }
    public bool IncludesAiAssistantModule { get; set; }
    public bool HasWhatsAppAI { get; set; }
    public bool HasDelivery { get; set; }
    public bool HasAutoPrint { get; set; }
    public bool HasBasicReports { get; set; }
    public bool HasManagementDashboard { get; set; }
    public bool HasAdvancedReports { get; set; }
    public bool HasCoupons { get; set; }
    public bool HasRecurringCustomers { get; set; }
    public bool IsCompanyActive { get; set; }
    public int OrdersToday { get; set; }
    public int DeliveryOrdersToday { get; set; }
    public int PaidOrdersToday { get; set; }
    public int DeletedOrdersToday { get; set; }
    public int OpenOrders { get; set; }
    public int PendingPayments { get; set; }
    public int FailedPrints { get; set; }
    public int PrintedToday { get; set; }
    public int TablesCount { get; set; }
    public int MenuItemsCount { get; set; }
    public int StockItemsCount { get; set; }
    public int TeamMembersCount { get; set; }
    public bool DeliveryEnabled { get; set; }
    public bool AiEnabled { get; set; }
    public bool AiConfigured { get; set; }
    public string AiModel { get; set; } = string.Empty;
    public int AiInteractionsToday { get; set; }
    public int SuccessfulAiInteractionsToday { get; set; }
    public DateTime? LastOrderAtUtc { get; set; }
    public bool HasMasterPassword { get; set; }
    public DateTime? MasterPasswordRotatedAtUtc { get; set; }
}

public class AdminCompanyPlanUpdateDto
{
    public Guid CompanyId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string PlanTier { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int MaxUsers { get; set; }
    public bool IncludesMenuModule { get; set; }
    public bool IncludesTablesModule { get; set; }
    public bool IncludesKitchenModule { get; set; }
    public bool IncludesCashModule { get; set; }
    public bool IncludesStockModule { get; set; }
    public bool IncludesDeliveryModule { get; set; }
    public bool IncludesPrintingModule { get; set; }
    public bool IncludesWaiterCallModule { get; set; }
    public bool IncludesAiAssistantModule { get; set; }
    public bool HasWhatsAppAI { get; set; }
    public bool HasDelivery { get; set; }
    public bool HasAutoPrint { get; set; }
    public bool HasBasicReports { get; set; }
    public bool HasManagementDashboard { get; set; }
    public bool HasAdvancedReports { get; set; }
    public bool HasCoupons { get; set; }
    public bool HasRecurringCustomers { get; set; }
}

public class AdminCompanyMasterPasswordStatusDto
{
    public Guid CompanyId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public bool HasMasterPassword { get; set; }
    public string MaskedPassword { get; set; } = string.Empty;
    public DateTime? RotatedAtUtc { get; set; }
}

public class AdminCompanyMasterPasswordRevealDto : AdminCompanyMasterPasswordStatusDto
{
    public string RawPassword { get; set; } = string.Empty;
}

public class DeleteAdminCompanyRequestDto : AdminSensitiveActionRequestDto
{
    public string ConfirmationText { get; set; } = string.Empty;
}
