namespace ZeroPaper.DTOs.Workspace;

public class SalesAgentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal? CommissionPercent { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateSalesAgentRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal? CommissionPercent { get; set; }
}

public class UpdateSalesAgentRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal? CommissionPercent { get; set; }
}

public class UpdateSalesAgentStatusRequestDto
{
    public bool IsActive { get; set; }
}

public class PublicSellerLinkDto
{
    public string SellerName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyLogoUrl { get; set; }
    public string CashTablePublicCode { get; set; } = string.Empty;
}
