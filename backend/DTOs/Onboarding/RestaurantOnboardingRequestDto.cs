using System.ComponentModel.DataAnnotations;

namespace ZeroPaper.DTOs.Onboarding;

public class RestaurantOnboardingRequestDto
{
    [Required]
    [MaxLength(150)]
    public string RestaurantName { get; set; } = string.Empty;

    [Required]
    [MaxLength(180)]
    public string LegalName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? TenantIdentifier { get; set; }

    [MaxLength(80)]
    public string? AccessSlug { get; set; }

    [Required]
    [MaxLength(150)]
    public string OwnerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(180)]
    public string OwnerEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string AccessCode { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string OwnerPassword { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? ContactPhone { get; set; }

    [Required]
    [MaxLength(120)]
    public string PlanName { get; set; } = "Plano Bairro";

    [Range(0, 999999)]
    public decimal MonthlyPrice { get; set; } = 149.90m;

    [Range(1, 500)]
    public int MaxUsers { get; set; } = 5;
}
