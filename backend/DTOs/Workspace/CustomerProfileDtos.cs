namespace ZeroPaper.DTOs.Workspace;

public class CustomerProfileDto
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? ZipCode { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Neighborhood { get; set; }
    public string? Complement { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime LastOrderAtUtc { get; set; }
}

public class UpdateCustomerProfileRequestDto
{
    public string? Name { get; set; }
    public string? ZipCode { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Neighborhood { get; set; }
    public string? Complement { get; set; }
}

public class CustomerOrderHistoryDto
{
    public Guid OrderId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public List<CustomerOrderHistoryItemDto> Items { get; set; } = [];
}

public class CustomerOrderHistoryItemDto
{
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public class PublicCustomerProfileDto
{
    public bool Found { get; set; }
    public string? Message { get; set; }
    public string? CustomerName { get; set; }
    public string? MaskedPhone { get; set; }
    public PublicCustomerPrimaryAddressDto? PrimaryAddress { get; set; }
    public string? BusinessName { get; set; }
    public string? BusinessSlug { get; set; }
    public bool CanEditProfile { get; set; }
    public bool CanReorder { get; set; }
    public bool HasActiveOrder { get; set; }
    public List<PublicCustomerRecentOrderDto> RecentOrders { get; set; } = [];
}

public class PublicCustomerPrimaryAddressDto
{
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Neighborhood { get; set; }
    public string? Complement { get; set; }
    public string? ZipCode { get; set; }
}

public class PublicCustomerRecentOrderDto
{
    public int? OrderNumber { get; set; }
    public string? DisplayCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string FulfillmentType { get; set; } = string.Empty;
    public List<PublicCustomerRecentOrderItemDto> Items { get; set; } = [];
}

public class PublicCustomerRecentOrderItemDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? Total { get; set; }
}
