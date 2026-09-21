namespace Findora.API.Models;

/// <summary>
/// Account status (Module 3). Enforced server-side in two places: new
/// tokens are refused for a non-<see cref="Active"/> account (see
/// <c>AuthService.LoginAsync</c>/<c>RefreshAsync</c>), and an
/// already-issued access token stops working immediately once the account
/// is no longer <see cref="Active"/> (see <c>Authorization/ActiveAccountAuthorizationHandler.cs</c>).
/// </summary>
public enum UserStatus
{
    Active,
    Suspended,
    Deactivated
}
