namespace ZeroPaper.DTOs.Admin;

public class AdminDemoAccountDto
{
    public bool Exists { get; set; }
    public Guid? CompanyId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public bool HasActiveLink { get; set; }
    public DateTime? LinkCreatedAtUtc { get; set; }
    public DateTime? LinkLastUsedAtUtc { get; set; }
    public int ActiveSessionCount { get; set; }
}

public class AdminDemoLinkCreatedDto : AdminDemoAccountDto
{
    public string AccessUrl { get; set; } = string.Empty;
}
