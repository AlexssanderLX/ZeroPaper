using ZeroPaper.Domain.Enums;

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
    public string PlanName { get; set; } = string.Empty;
    public string PlanTier { get; set; } = string.Empty;
    public bool IncludesMenuModule { get; set; } = true;
    public bool IncludesTablesModule { get; set; } = true;
    public bool IncludesKitchenModule { get; set; } = true;
    public bool IncludesCashModule { get; set; } = true;
    public bool IncludesStockModule { get; set; } = true;
    public bool IncludesDeliveryModule { get; set; } = true;
    public bool IncludesPrintingModule { get; set; } = true;
    public bool IncludesWaiterCallModule { get; set; } = true;
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

public class DiningTableDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public string? ComandaLabel { get; set; }
    public bool IsDeliveryChannel { get; set; }
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
    public string? ComandaLabel { get; set; }
}

public class UpdateDiningTableRequestDto
{
    public string Name { get; set; } = string.Empty;
    public int Seats { get; set; }
    public string? ComandaLabel { get; set; }
}

public class MenuCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public List<MenuItemDto> Items { get; set; } = [];
}

public class MenuCategorySummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public int TotalItems { get; set; }
    public int ActiveItems { get; set; }
    public int HiddenItems { get; set; }
    public int ItemsWithoutImage { get; set; }
    public int ItemsWithAdditionals { get; set; }
    public decimal? StartingPrice { get; set; }
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
    public decimal StartingPrice { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int? MaxAdditionalSelections { get; set; }
    public bool HasAdditionalOptions { get; set; }

    public List<MenuItemAdditionalGroupDto> AdditionalGroups { get; set; } = [];
}

public class MenuItemAdditionalGroupDto
{
    public Guid Id { get; set; }
    public Guid? SourceMenuAdditionalCatalogGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool AllowMultiple { get; set; }
    public int DisplayOrder { get; set; }
    public int? MaxAdditionalSelections { get; set; }
    public List<MenuItemAdditionalOptionDto> Options { get; set; } = [];
}

public class MenuItemAdditionalOptionDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid? SourceMenuAdditionalCatalogOptionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DisplayOrder { get; set; }
}

public class MenuAdditionalCatalogGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool AllowMultiple { get; set; }
    public int DisplayOrder { get; set; }
    public int? MaxAdditionalSelections { get; set; }
    public int LinkedItemCount { get; set; }
    public List<string> LinkedItemNames { get; set; } = [];
    public List<MenuAdditionalCatalogOptionDto> Options { get; set; } = [];
}

public class MenuAdditionalCatalogOptionDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateMenuCategoryRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class UpdateMenuCategoryRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class CreateMenuItemRequestDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AccentLabel { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int? MaxAdditionalSelections { get; set; }
    public List<Guid> AdditionalCatalogGroupIds { get; set; } = [];
    public List<MenuItemAdditionalGroupInputDto> AdditionalGroups { get; set; } = [];
}

public class UpdateMenuItemRequestDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AccentLabel { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int? MaxAdditionalSelections { get; set; }

    public List<Guid> AdditionalCatalogGroupIds { get; set; } = [];
    public List<MenuItemAdditionalGroupInputDto> AdditionalGroups { get; set; } = [];
}

public class MenuItemAdditionalGroupInputDto
{
    public Guid? CatalogGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool AllowMultiple { get; set; }
    public int? MaxAdditionalSelections { get; set; }
    public List<MenuItemAdditionalOptionInputDto> Options { get; set; } = [];
}

public class MenuItemAdditionalOptionInputDto
{
    public Guid? CatalogOptionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class SaveMenuAdditionalCatalogGroupRequestDto
{
    public string Name { get; set; } = string.Empty;
    public bool AllowMultiple { get; set; }
    public int? MaxAdditionalSelections { get; set; }
    public List<SaveMenuAdditionalCatalogOptionRequestDto> Options { get; set; } = [];
}

public class SaveMenuAdditionalCatalogOptionRequestDto
{
    public string Name { get; set; } = string.Empty;
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
    public List<Guid> AdditionalOptionIds { get; set; } = [];
}

public class CreateCustomerOrderRequestDto
{
    public Guid? TableId { get; set; }
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    public string? DeliveryPhone { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryNumber { get; set; }
    public string? DeliveryNeighborhood { get; set; }
    public string? DeliveryComplement { get; set; }
    public string? DeliveryPostalCode { get; set; }
    public string? FulfillmentType { get; set; }
    public string? PaymentMethod { get; set; }
    public string? CouponCode { get; set; }
    public OrderPaymentMode PaymentMode { get; set; } = OrderPaymentMode.PayAfterEating;
    public List<OrderItemInputDto> Items { get; set; } = [];
    public List<MenuOrderSelectionDto> MenuSelections { get; set; } = [];
}

public class CouponDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool IsActive { get; set; }
    public int? UsageLimit { get; set; }
    public int UsageCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class SaveCouponRequestDto
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal MinimumOrderAmount { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public int? UsageLimit { get; set; }
}

public class UpdateCouponStatusRequestDto
{
    public bool IsActive { get; set; }
}

public class ValidateCouponRequestDto
{
    public string Code { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
}

public class CouponValidationDto
{
    public bool IsValid { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Message { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalSubtotal { get; set; }
    public CouponDto? Coupon { get; set; }
}

public class CashClosingReportDto
{
    public DateOnly ReferenceDate { get; set; }
    public decimal TotalSold { get; set; }
    public int OrdersCount { get; set; }
    public decimal AverageTicket { get; set; }
    public decimal DiscountsTotal { get; set; }
    public int CancelledOrdersCount { get; set; }
    public IReadOnlyList<CashClosingPaymentMethodDto> PaymentMethods { get; set; } = [];
}

public class CashClosingPaymentMethodDto
{
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int OrdersCount { get; set; }
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid? MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Quantity { get; set; }
    public decimal BaseUnitPrice { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public List<OrderItemAdditionalSelectionDto> AdditionalSelections { get; set; } = [];
}

public class OrderItemAdditionalSelectionDto
{
    public Guid? SourceMenuItemAdditionalOptionId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string OptionName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}

public class CustomerOrderDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public Guid TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string? PublicCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string RequestedPaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PrintStatus { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    public bool IsDeliveryOrder { get; set; }
    public string FulfillmentType { get; set; } = string.Empty;
    public string? DeliveryPhone { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryNumber { get; set; }
    public string? DeliveryNeighborhood { get; set; }
    public string? DeliveryComplement { get; set; }
    public string? DeliveryPostalCode { get; set; }
    public decimal DeliveryFreightAmount { get; set; }
    public decimal? DeliveryDistanceKm { get; set; }
    public string? DeliveryFreightProvider { get; set; }
    public DateTime? DeliveryFreightCalculatedAtUtc { get; set; }
    public bool CanEditPublicly { get; set; }
    public DateTime? PublicEditAllowedUntilUtc { get; set; }
    public string? PublicEditUrl { get; set; }
    public string? PublicDeliveryCustomerUrl { get; set; }
    public string? DeliveryAssistantMessage { get; set; }
    public decimal OriginalTotalAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalItemQuantity { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAtUtc { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public Guid? CouponId { get; set; }
    public string? CouponCode { get; set; }
    public decimal CouponDiscountAmount { get; set; }
    public DateTime? CouponAppliedAtUtc { get; set; }
    public string? PriceAdjustmentNote { get; set; }
    public DateTime? PriceAdjustedAtUtc { get; set; }
    public bool HasPriceAdjustment { get; set; }
    public decimal PaymentTotalAmount { get; set; }
    public decimal RemainingPaymentAmount { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? PrintedAtUtc { get; set; }
    public int PrintAttempts { get; set; }
    public string? PrintLastError { get; set; }
    public string? PrintAgentName { get; set; }
    public string? PrintPrinterName { get; set; }
    public List<OrderPaymentDto> Payments { get; set; } = [];
    public List<OrderItemDto> Items { get; set; } = [];
}

public class OrderPaymentDto
{
    public Guid Id { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
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
    public List<OrderPaymentInputDto> Payments { get; set; } = [];
}

public class OrderPaymentInputDto
{
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class MarkOrderPaidInputDto
{
    public Guid OrderId { get; set; }
    public string? PaymentMethod { get; set; }
    public List<OrderPaymentInputDto> Payments { get; set; } = [];
}

public class MarkAllOrdersPaidRequestDto
{
    public List<MarkOrderPaidInputDto> Orders { get; set; } = [];
}

public class MarkAllOrdersPaidResultDto
{
    public int MarkedCount { get; set; }
    public int IgnoredCount { get; set; }
    public List<string> IgnoredReasons { get; set; } = [];
}

public class UpdateCustomerOrderItemRequestDto
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCustomerOrderRequestDto
{
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    public string? DeliveryPhone { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryNumber { get; set; }
    public string? DeliveryNeighborhood { get; set; }
    public string? DeliveryComplement { get; set; }
    public string? DeliveryPostalCode { get; set; }
    public string? FulfillmentType { get; set; }
    public string? PaymentMethod { get; set; }
    public List<UpdateCustomerOrderItemRequestDto> Items { get; set; } = [];
    public List<MenuOrderSelectionDto> MenuSelections { get; set; } = [];
}

public class AdjustOrderValueRequestDto
{
    public decimal? FinalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public string? Note { get; set; }
}

public class DeletePaidOrderRequestDto
{
    public string Password { get; set; } = string.Empty;
}

public class OwnerPasswordRequestDto
{
    public string Password { get; set; } = string.Empty;
}

public class DeliveryFreightSettingsDto
{
    public bool IsEnabled { get; set; }
    public string? OriginPostalCode { get; set; }
    public decimal PricePerKm { get; set; }
    public decimal BaseFee { get; set; }
    public decimal BaseDistanceKm { get; set; }
    public string Provider { get; set; } = string.Empty;
    public bool ProviderConfigured { get; set; }
    public bool IsTestMode { get; set; }
    public int CacheDays { get; set; }
    public int? PickupEstimatedMinutes { get; set; }
    public int? DeliveryEstimatedMinutes { get; set; }
}

public class UpdateDeliveryFreightSettingsRequestDto
{
    public bool IsEnabled { get; set; }
    public string? OriginPostalCode { get; set; }
    public decimal PricePerKm { get; set; }
    public decimal BaseFee { get; set; }
    public decimal BaseDistanceKm { get; set; }
    public int? PickupEstimatedMinutes { get; set; }
    public int? DeliveryEstimatedMinutes { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class DeliveryFreightQuoteRequestDto
{
    public string? DestinationPostalCode { get; set; }
    public decimal Subtotal { get; set; }
}

public class DeliveryFreightQuoteDto
{
    public bool IsEnabled { get; set; }
    public bool IsConfigured { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsTestMode { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? OriginPostalCode { get; set; }
    public string? DestinationPostalCode { get; set; }
    public decimal? DistanceKm { get; set; }
    public decimal BaseFee { get; set; }
    public decimal BaseDistanceKm { get; set; }
    public decimal ChargedDistanceKm { get; set; }
    public decimal PricePerKm { get; set; }
    public decimal FreightAmount { get; set; }
    public decimal TotalWithFreight { get; set; }
    public bool FromCache { get; set; }
    public string? Message { get; set; }
}

public class PublicDeliveryCustomerProfileDto
{
    public bool Found { get; set; }
    public string? CustomerName { get; set; }
    public string? DeliveryPhone { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryNumber { get; set; }
    public string? DeliveryNeighborhood { get; set; }
    public string? DeliveryComplement { get; set; }
    public string? DeliveryPostalCode { get; set; }
    public DateTime? LastOrderAtUtc { get; set; }
    public string? Message { get; set; }
}

public class PublicDeliveryShortLinkDto
{
    public bool Found { get; set; }
    public string? PublicCode { get; set; }
    public string? CustomerToken { get; set; }
    public string? Message { get; set; }
}

public class PublicOrderTrackingDto
{
    public bool Found { get; set; }
    public string? Message { get; set; }
    public string? RestaurantName { get; set; }
    public PublicTrackedOrderDto? Order { get; set; }
}

public class PublicTrackedOrderDto
{
    public int Number { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal TotalItemQuantity { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public bool IsEdited { get; set; }
    public DateTime? EditedAtUtc { get; set; }
    public List<PublicTrackedOrderItemDto> Items { get; set; } = [];
}

public class PublicTrackedOrderItemDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public List<OrderItemAdditionalSelectionDto> AdditionalSelections { get; set; } = [];
}

public class PublicEditShortLinkDto
{
    public bool Found { get; set; }
    public bool IsExpired { get; set; }
    public string? PublicCode { get; set; }
    public string? EditCode { get; set; }
    public DateTime? PublicEditAllowedUntilUtc { get; set; }
    public string? Message { get; set; }
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
    public string? LogoUrl { get; set; }
    public string AccessSlug { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public AlertSettingsDto Alerts { get; set; } = new();
    public OwnerShortcutAccessDto ShortcutAccess { get; set; } = new();
}

public class UpdateCompanySettingsRequestDto
{
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class OwnerProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UpdateOwnerProfileRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ChangeOwnerPasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class OwnerShortcutAccessDto
{
    public bool IsEnabled { get; set; }
    public DateTime? CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
}

public class GenerateOwnerShortcutAccessRequestDto
{
    public string Password { get; set; } = string.Empty;
}

public class GenerateOwnerShortcutAccessResponseDto
{
    public OwnerShortcutAccessDto ShortcutAccess { get; set; } = new();
    public string RawToken { get; set; } = string.Empty;
    public string ShortcutUrl { get; set; } = string.Empty;
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
    public bool AutoPrintEnabled { get; set; }
    public bool CanAutoPrint { get; set; }
    public string PaperProfile { get; set; } = string.Empty;
    public int OrdersPerPage { get; set; }
    public bool HasAgentKey { get; set; }
    public bool HasAgentToken { get; set; }
    public Guid? AgentId { get; set; }
    public bool AgentOnline { get; set; }
    public string? AgentName { get; set; }
    public string? PrinterName { get; set; }
    public string? AppVersion { get; set; }
    public DateTime? RegisteredAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastErrorAtUtc { get; set; }
    public int PendingJobs { get; set; }
    public int FailedJobs { get; set; }
    public int PrintedJobs { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string DownloadUrlX86 { get; set; } = string.Empty;
    public string DownloadUrlX64 { get; set; } = string.Empty;
    public string LegacyDownloadUrl { get; set; } = string.Empty;
    public List<PrintOrderSummaryDto> RecentOrders { get; set; } = [];
}

public class PrintOrderSummaryDto
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public bool IsDeliveryOrder { get; set; }
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
    public string AgentToken { get; set; } = string.Empty;
    public PrintingSettingsDto Printing { get; set; } = new();
}

public class CreatePrintTestJobRequestDto
{
    public string? Notes { get; set; }
}

public class PrintTestJobResponseDto
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime QueuedAtUtc { get; set; }
    public PrintingSettingsDto Printing { get; set; } = new();
}

public class PrintAgentRegistrationResponseDto
{
    public Guid AgentId { get; set; }
    public Guid CompanyId { get; set; }
    public bool AutoPrintEnabled { get; set; }
    public string PaperProfile { get; set; } = string.Empty;
    public int OrdersPerPage { get; set; }
    public DateTime RegisteredAtUtc { get; set; }
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
    public decimal BaseUnitPrice { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public List<PrintAgentOrderAdditionalDto> Additionals { get; set; } = [];
}

public class PrintAgentOrderAdditionalDto
{
    public string GroupName { get; set; } = string.Empty;
    public string OptionName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}

public class PrintAgentOrderJobDto
{
    public Guid OrderId { get; set; }
    public Guid JobId { get; set; }
    public string JobKind { get; set; } = "Order";
    public bool IsTest { get; set; }
    public int Number { get; set; }
    public string PaperProfile { get; set; } = string.Empty;
    public int OrdersPerPage { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? DeliveryPhone { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryNumber { get; set; }
    public string? DeliveryComplement { get; set; }
    public string? DeliveryPostalCode { get; set; }
    public decimal DeliveryFreightAmount { get; set; }
    public decimal? DeliveryDistanceKm { get; set; }
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
    public string? RestaurantLogoUrl { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string AccessCode { get; set; } = string.Empty;
    public bool IsDeliveryChannel { get; set; }
    public bool IsOnlinePaymentAvailable { get; set; }
    public int DeliveryEditWindowMinutes { get; set; }
    public bool IsOrderingAvailable { get; set; } = true;
    public string? OrderingUnavailableMessage { get; set; }
    public List<int>? ServiceDays { get; set; }
    public string? ServiceStartTime { get; set; }
    public string? ServiceEndTime { get; set; }
    public int? PickupEstimatedMinutes { get; set; }
    public int? DeliveryEstimatedMinutes { get; set; }
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
