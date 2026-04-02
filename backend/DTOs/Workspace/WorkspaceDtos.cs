namespace ZeroPaper.DTOs.Workspace;

public class WorkspaceOverviewDto
{
    public int ActiveTables { get; set; }
    public int OpenOrders { get; set; }
    public int PublishedMenuItems { get; set; }
    public int TotalMenuItems { get; set; }
    public int TotalStockItems { get; set; }
    public int LowStockItems { get; set; }
    public int PendingPayments { get; set; }
    public int PendingPrints { get; set; }
    public int PrintedPrints { get; set; }
    public int FailedPrints { get; set; }
}

public class DiningTableDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public int Seats { get; set; }
    public string Status { get; set; } = string.Empty;
    public int OpenOrderCount { get; set; }
    public string PublicCode { get; set; } = string.Empty;
    public string AccessUrl { get; set; } = string.Empty;
    public string? AlertSoundUrl { get; set; }
    public bool HasCustomAlertSound { get; set; }
}

public class CreateDiningTableRequestDto
{
    public string Name { get; set; } = string.Empty;
    public int Seats { get; set; }
}

public class UpdateDiningTableRequestDto
{
    public string Name { get; set; } = string.Empty;
    public int Seats { get; set; }
}

public class MenuCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<MenuItemDto> Items { get; set; } = [];
}

public class MenuItemDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AccentLabel { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateMenuCategoryRequestDto
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateMenuCategoryRequestDto
{
    public string Name { get; set; } = string.Empty;
}

public class CreateMenuItemRequestDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AccentLabel { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
}

public class UpdateMenuItemRequestDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AccentLabel { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
}

public class UpdateMenuItemStatusRequestDto
{
    public bool IsActive { get; set; }
}

public class UploadMenuItemImageResponseDto
{
    public string ImageUrl { get; set; } = string.Empty;
}

public class OrderItemInputDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}

public class MenuOrderSelectionDto
{
    public Guid MenuItemId { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}

public class CreateCustomerOrderRequestDto
{
    public Guid? TableId { get; set; }
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    public string? PaymentMethod { get; set; }
    public List<OrderItemInputDto> Items { get; set; } = [];
    public List<MenuOrderSelectionDto> MenuSelections { get; set; } = [];
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
}

public class CustomerOrderDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public Guid TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string RequestedPaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PrintStatus { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? PrintedAtUtc { get; set; }
    public int PrintAttempts { get; set; }
    public string? PrintLastError { get; set; }
    public string? PrintAgentName { get; set; }
    public string? PrintPrinterName { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}

public class UpdateOrderStatusRequestDto
{
    public string Status { get; set; } = string.Empty;
    public string? Password { get; set; }
}

public class BatchUpdateOrderStatusRequestDto
{
    public string Status { get; set; } = string.Empty;
    public string? Password { get; set; }
    public List<Guid> OrderIds { get; set; } = [];
}

public class UpdateOrderPaymentRequestDto
{
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
}

public class DeletePaidOrderRequestDto
{
    public string Password { get; set; } = string.Empty;
}

public class OwnerPasswordRequestDto
{
    public string Password { get; set; } = string.Empty;
}

public class BatchDeleteClosedOrdersRequestDto
{
    public string? Password { get; set; }
    public List<Guid> OrderIds { get; set; } = [];
}

public class StockItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal MinimumQuantity { get; set; }
    public bool IsLowStock { get; set; }
}

public class SaveStockItemRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal MinimumQuantity { get; set; }
}

public class TeamMemberDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}

public class CreateTeamMemberRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class CompanySettingsDto
{
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string AccessSlug { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public AlertSettingsDto Alerts { get; set; } = new();
}

public class UpdateCompanySettingsRequestDto
{
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class AlertSettingsDto
{
    public bool EnableOrderAlerts { get; set; }
    public bool EnableWaiterCallAlerts { get; set; }
    public string? SoundUrl { get; set; }
    public bool HasCustomSound { get; set; }
    public int VolumePercent { get; set; }
    public int PlaybackSeconds { get; set; }
}

public class PrintingSettingsDto
{
    public bool EnableAutomaticPrinting { get; set; }
    public string PaperProfile { get; set; } = string.Empty;
    public int OrdersPerPage { get; set; }
    public bool HasAgentKey { get; set; }
    public bool AgentOnline { get; set; }
    public string? AgentName { get; set; }
    public string? PrinterName { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public int PendingJobs { get; set; }
    public int FailedJobs { get; set; }
    public int PrintedJobs { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public List<PrintOrderSummaryDto> RecentOrders { get; set; } = [];
}

public class PrintOrderSummaryDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PrintStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? PrintedAtUtc { get; set; }
    public int PrintAttempts { get; set; }
    public string? PrintLastError { get; set; }
}

public class UpdatePrintingSettingsRequestDto
{
    public bool EnableAutomaticPrinting { get; set; }
    public string PaperProfile { get; set; } = string.Empty;
    public int OrdersPerPage { get; set; } = 1;
}

public class RotatePrintingAgentKeyResponseDto
{
    public string AgentKey { get; set; } = string.Empty;
    public PrintingSettingsDto Printing { get; set; } = new();
}

public class PrintAgentHeartbeatRequestDto
{
    public string AgentName { get; set; } = string.Empty;
    public string? PrinterName { get; set; }
    public string? AppVersion { get; set; }
}

public class PrintAgentClaimRequestDto
{
    public string AgentName { get; set; } = string.Empty;
    public string? PrinterName { get; set; }
}

public class PrintAgentOrderItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
}

public class PrintAgentOrderJobDto
{
    public Guid OrderId { get; set; }
    public int Number { get; set; }
    public string PaperProfile { get; set; } = string.Empty;
    public int OrdersPerPage { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PrintAgentOrderItemDto> Items { get; set; } = [];
}

public class CompletePrintJobRequestDto
{
    public string AgentName { get; set; } = string.Empty;
    public string? PrinterName { get; set; }
}

public class FailPrintJobRequestDto
{
    public string AgentName { get; set; } = string.Empty;
    public string? PrinterName { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CompletePrintJobBatchRequestDto
{
    public string AgentName { get; set; } = string.Empty;
    public string? PrinterName { get; set; }
    public List<Guid> OrderIds { get; set; } = [];
}

public class FailPrintJobBatchRequestDto
{
    public string AgentName { get; set; } = string.Empty;
    public string? PrinterName { get; set; }
    public string? ErrorMessage { get; set; }
    public List<Guid> OrderIds { get; set; } = [];
}

public class UpdateAlertSettingsRequestDto
{
    public bool EnableOrderAlerts { get; set; }
    public bool EnableWaiterCallAlerts { get; set; }
    public int VolumePercent { get; set; }
    public int PlaybackSeconds { get; set; }
}

public class UploadAlertSoundResponseDto
{
    public AlertSettingsDto Alerts { get; set; } = new();
}

public class UploadTableAlertSoundResponseDto
{
    public DiningTableDto Table { get; set; } = new();
}

public class PublicTableViewDto
{
    public string RestaurantName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string AccessCode { get; set; } = string.Empty;
    public List<MenuCategoryDto> Menu { get; set; } = [];
}

public class WaiterCallDto
{
    public Guid Id { get; set; }
    public Guid TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string? TableAlertSoundUrl { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

public class WorkspaceAlertsSignalDto
{
    public int PendingWaiterCalls { get; set; }
    public DateTime? LatestWaiterCallAtUtc { get; set; }
    public string? LatestWaiterCallTableName { get; set; }
    public string? LatestWaiterCallTableSoundUrl { get; set; }
    public DateTime? LatestOrderAtUtc { get; set; }
}
