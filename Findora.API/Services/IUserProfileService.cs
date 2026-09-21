using Findora.API.DTOs.Users;
using Findora.API.Models;

namespace Findora.API.Services;

/// <summary>
/// Profile/account-status business logic (Module 3). Kept out of
/// <c>UsersController</c> so it stays thin, mirroring the
/// <c>AuthController</c>/<c>IAuthService</c> split from Module 2.
/// </summary>
public interface IUserProfileService
{
    Task<ServiceResult<UserProfileResponse>> GetOwnProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ServiceResult<UserProfileResponse>> UpdateOwnProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<PublicUserResponse>> GetPublicProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminUserStatusResponse>> UpdateStatusAsync(Guid userId, UserStatus newStatus, CancellationToken cancellationToken = default);
}
