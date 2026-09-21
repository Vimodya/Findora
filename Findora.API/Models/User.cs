namespace Findora.API.Models;

/// <summary>
/// An authenticated Findora account. Module 2 added the authentication
/// fields; Module 3 adds the profile/account-status/reputation fields
/// below. Lost/found report history (also scoped to Module 3 conceptually)
/// is deliberately NOT a field or table here — it will be a query surface
/// over <c>LostReports</c>/<c>FoundReports</c> once Modules 4/5 introduce
/// those entities, not something this entity anticipates with a placeholder.
/// </summary>
public class User : AuditableEntity
{
    /// <summary>User-facing email address, as entered (original casing).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Lowercased/trimmed <see cref="Email"/>, used for case-insensitive
    /// uniqueness checks and lookups without depending on database collation.
    /// </summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>Display name — doubles as the profile's "name" field (Module 3 reuses this rather than adding a duplicate).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Hash produced by <see cref="Microsoft.AspNetCore.Identity.IPasswordHasher{TUser}"/>.
    /// Never a plaintext password.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }

    /// <summary>Optional contact phone number (Module 3).</summary>
    public string? Phone { get; set; }

    /// <summary>Optional avatar image URL (Module 3).</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Account status (Module 3). Server-enforced — see
    /// <see cref="UserStatus"/>'s doc comment for where. Only an Admin may
    /// change this (<c>PATCH /api/v1/users/{id}/status</c>); it is never
    /// part of the self-service profile update.
    /// </summary>
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>
    /// Reputation schema placeholder (Module 3) — a numeric score, not yet
    /// computed by any algorithm (that's Module 17) and not user-editable.
    /// Defaults to 0 for every new account.
    /// </summary>
    public int ReputationScore { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
