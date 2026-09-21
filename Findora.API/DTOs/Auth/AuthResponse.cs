namespace Findora.API.DTOs.Auth;

/// <summary>Response for a successful login or refresh.</summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary>Opaque refresh token — store it securely client-side; never logged server-side.</summary>
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }

    public UserSummaryResponse User { get; set; } = new();
}
