namespace ZeroPaper.DTOs.Workspace;

public class WorkspaceOverviewDto
{
    public int ActiveTables { get; set; }
    public int OpenOrders { get; set; }
    public int LowStockItems { get; set; }
    public int TeamMembers { get; set; }
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
}

public class CreateDiningTableRequestDto
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

public class CreateMenuItemRequestDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AccentLabel { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
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
    public List<OrderItemInputDto> Items { get; set; } = [];
    public List<MenuOrderSelectionDto> MenuSelections { get; set; } = [];
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
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
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}

public class UpdateOrderStatusRequestDto
{
    public string Status { get; set; } = string.Empty;
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
}

public class UpdateCompanySettingsRequestDto
{
    public string LegalName { get; set; } = string.Empty;
    public string TradeName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class PublicTableViewDto
{
    public string RestaurantName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string AccessCode { get; set; } = string.Empty;
    public List<MenuCategoryDto> Menu { get; set; } = [];
}
