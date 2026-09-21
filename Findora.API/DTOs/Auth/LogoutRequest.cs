using System.ComponentModel.DataAnnotations;

namespace Findora.API.DTOs.Auth;

public class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
