namespace Findora.API.DTOs.Auth;

/// <summary>
/// Response for a successful registration. Deliberately does not include
/// auth tokens — registration and login are separate steps; the caller
/// logs in afterward (see <c>AuthController.Login</c>).
/// </summary>
public class RegisterResponse
{
    public string Message { get; set; } = string.Empty;
    public UserSummaryResponse User { get; set; } = new();
}
