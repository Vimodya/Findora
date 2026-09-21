using System.ComponentModel.DataAnnotations;

namespace Findora.API.DTOs.Auth;

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
