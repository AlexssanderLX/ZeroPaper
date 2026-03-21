namespace ZeroPaper.DTOs.Auth;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Profile { get; set; }
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

public class PasswordResetRequestDto
{
    public string Email { get; set; } = string.Empty;
}

public class PasswordResetRequestResponseDto
{
    public bool Accepted { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ConfirmPasswordRequestDto
{
    public string Password { get; set; } = string.Empty;
}

public class ConfirmPasswordResponseDto
{
    public bool Confirmed { get; set; }
}
