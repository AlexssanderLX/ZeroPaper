namespace ZeroPaper.DTOs.Admin;

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsCompanyActive { get; set; }
    public bool HasActiveSession { get; set; }
    public bool IsOnlineNow { get; set; }
    public int ActiveSessionCount { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
}
