namespace ZeroPaper.DTOs.Auth;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Profile { get; set; } = "restaurant";
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string Email { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;
}

