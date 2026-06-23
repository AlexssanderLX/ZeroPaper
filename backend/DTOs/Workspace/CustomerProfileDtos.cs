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
