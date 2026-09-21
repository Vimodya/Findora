using Findora.API.DTOs.Users;
using Findora.API.Models;

namespace Findora.API.Mappings;

/// <summary>
/// Hand-written entity-to-DTO mapping for the user-profile surface (Module
/// 3) — mirrors <c>AuthMappingExtensions</c>' approach: small and stable
/// enough that a mapping library would be overkill.
/// </summary>
public static class UserMappingExtensions
{
    /// <summary>Maps to the authenticated user's own, richer profile view.</summary>
    public static UserProfileResponse ToProfileResponse(this User user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            IsEmailVerified = user.IsEmailVerified,
            Status = user.Status,
            ReputationScore = user.ReputationScore,
            Roles = user.UserRoles
                .Where(ur => ur.Role is not null)
                .Select(ur => ur.Role.Name)
                .ToArray(),
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>Maps to the limited, anonymous-safe public view.</summary>
    public static PublicUserResponse ToPublicResponse(this User user)
    {
        return new PublicUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            MemberSince = user.CreatedAt
        };
    }
}
