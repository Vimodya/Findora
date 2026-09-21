using Findora.API.Models;

namespace Findora.API.DTOs.Users;

/// <summary>
/// Shape returned by <c>GET/PUT /api/v1/users/me</c> — the authenticated
/// user's own view of their account. Deliberately richer than the public
/// profile (includes email, phone, status, reputation), but still never
/// exposes <c>PasswordHash</c> or any token data.
/// </summary>
public class UserProfileResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsEmailVerified { get; set; }

    /// <summary>Admin-controlled — never settable via the profile update endpoint.</summary>
    public UserStatus Status { get; set; }

    /// <summary>
    /// Schema-only placeholder (Module 3) — always the default until
    /// Module 17 implements real scoring. Never settable by the user.
    /// </summary>
    public int ReputationScore { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    public DateTime CreatedAt { get; set; }
}
