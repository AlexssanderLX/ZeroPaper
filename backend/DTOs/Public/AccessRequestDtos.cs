using System.ComponentModel.DataAnnotations;

namespace ZeroPaper.DTOs.Public;

public class AccessRequestDto
{
    [Required]
    [MaxLength(150)]
    public string RestaurantName { get; set; } = string.Empty;

    [MaxLength(180)]
    public string? LegalName { get; set; }

    [Required]
    [MaxLength(150)]
    public string OwnerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(180)]
    public string OwnerEmail { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? ContactPhone { get; set; }

    [MaxLength(120)]
    public string? CityRegion { get; set; }

    [MaxLength(800)]
    public string? Notes { get; set; }
}

public class AccessRequestResponseDto
{
    public bool Accepted { get; set; }
    public string Message { get; set; } = string.Empty;
}
