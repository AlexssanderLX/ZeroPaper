using System.ComponentModel.DataAnnotations;

namespace ZeroPaper.DTOs.Auth;

public class LoginRequestDto
{
    [Required]
    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(20)]
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

public class ShortcutLoginRequestDto
{
    [Required]
    [MaxLength(256)]
    public string Token { get; set; } = string.Empty;
}

public class PasswordResetRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;
}

public class PasswordResetRequestResponseDto
{
    public bool Accepted { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required]
    [MaxLength(256)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;
}

public class ConfirmPasswordRequestDto
{
    [Required]
    [MaxLength(256)]
    public string Password { get; set; } = string.Empty;
}

public class ConfirmPasswordResponseDto
{
    public bool Confirmed { get; set; }
}
