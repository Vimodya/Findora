using System.ComponentModel.DataAnnotations;

namespace Findora.API.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [MaxLength(100)]
    public string NewPassword { get; set; } = string.Empty;
}
