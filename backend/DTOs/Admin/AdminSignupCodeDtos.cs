namespace ZeroPaper.DTOs.Admin;

public class CreateSignupCodeRequestDto
{
    public string Label { get; set; } = "Cadastro manual";
    public string? BoundEmail { get; set; }
    public int ExpiresInDays { get; set; } = 7;
    public int MaxUses { get; set; } = 1;
    public string? AllowedPlanName { get; set; }
    public int? AllowedMaxUsers { get; set; }
}

public class SignupCodeDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? BoundEmail { get; set; }
    public string? AllowedPlanName { get; set; }
    public int? AllowedMaxUsers { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public int MaxUses { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateSignupCodeResponseDto : SignupCodeDto
{
    public string RawCode { get; set; } = string.Empty;
}
