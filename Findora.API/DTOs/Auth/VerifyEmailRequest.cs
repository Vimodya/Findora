using System.ComponentModel.DataAnnotations;

namespace Findora.API.DTOs.Auth;

public class VerifyEmailRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
