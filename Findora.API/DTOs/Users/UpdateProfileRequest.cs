using System.ComponentModel.DataAnnotations;

namespace Findora.API.DTOs.Users;

/// <summary>
/// Body for <c>PUT /api/v1/users/me</c>. Deliberately only carries the
/// fields a user may self-edit — there is no <c>Id</c>, <c>Email</c>,
/// <c>PasswordHash</c>, <c>Roles</c>, <c>Status</c>, or
/// <c>ReputationScore</c> property here at all, so those simply cannot be
/// set through this endpoint no matter what a client sends (the service
/// only ever reads the fields declared below).
/// </summary>
public class UpdateProfileRequest
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    // No [Phone]/[Url] format attributes: both would reject an empty string
    // (rather than treat it as "clear this field"), which is exactly how a
    // client would signal removal. UserProfileService normalizes
    // whitespace-only input to null and applies a light, forgiving format
    // check instead.
    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(2048)]
    public string? AvatarUrl { get; set; }
}
