namespace ZeroPaper.DTOs.Admin;

public class AdminOwnerDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string AccessSlug { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
    public bool IsCompanyActive { get; set; }
    public bool HasActiveSession { get; set; }
    public int ActiveSessionCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
}

public class CreateAdminOwnerRequestDto
{
    public Guid CompanyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string OwnerPassword { get; set; } = string.Empty;
    public string RootPassword { get; set; } = string.Empty;
}

public class UpdateAdminOwnerRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RootPassword { get; set; } = string.Empty;
}

public class ResetAdminOwnerPasswordRequestDto
{
    public string NewPassword { get; set; } = string.Empty;
    public string RootPassword { get; set; } = string.Empty;
}

public class ChangeAdminOwnerStatusRequestDto
{
    public string RootPassword { get; set; } = string.Empty;
}

public class HardDeleteAdminOwnerRequestDto
{
    public string RootPassword { get; set; } = string.Empty;
    public string ConfirmationText { get; set; } = string.Empty;
}
