using System.ComponentModel.DataAnnotations;

namespace Findora.API.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;
}
