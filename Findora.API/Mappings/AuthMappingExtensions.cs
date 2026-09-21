using Findora.API.DTOs.Auth;
using Findora.API.Models;

namespace Findora.API.Mappings;

/// <summary>
/// Hand-written entity-to-DTO mapping for the auth surface. Small and
/// stable enough that a mapping library (AutoMapper etc.) would be
/// overkill — kept in one place so controllers/services don't duplicate it.
/// </summary>
public static class AuthMappingExtensions
{
    public static UserSummaryResponse ToSummaryResponse(this User user)
    {
        return new UserSummaryResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            IsEmailVerified = user.IsEmailVerified,
            Roles = user.UserRoles
                .Where(ur => ur.Role is not null)
                .Select(ur => ur.Role.Name)
                .ToArray()
        };
    }
}
