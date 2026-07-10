using System.ComponentModel.DataAnnotations;

namespace ZeroPaper.DTOs.Public;

public class ContactMessageDto
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = "";

    [MaxLength(30)]
    public string? Phone { get; set; }

    [Required, MaxLength(2000)]
    public string Message { get; set; } = "";
}

public class ContactMessageResponseDto
{
    public bool Sent { get; set; }
    public string Info { get; set; } = "";
}
