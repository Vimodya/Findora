using Findora.API.Data;
using Findora.API.DTOs.Users;
using Findora.API.Mappings;
using Findora.API.Models;
using Findora.API.Repositories;

namespace Findora.API.Services;

public class UserProfileService : IUserProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly FindoraDbContext _db;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(IUserRepository userRepository, FindoraDbContext db, ILogger<UserProfileService> logger)
    {
        _userRepository = userRepository;
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceResult<UserProfileResponse>> GetOwnProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<UserProfileResponse>.Fail(ServiceErrorCode.NotFound, "User not found.");
        }

        return ServiceResult<UserProfileResponse>.Success(user.ToProfileResponse());
    }

    public async Task<ServiceResult<UserProfileResponse>> UpdateOwnProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<UserProfileResponse>.Fail(ServiceErrorCode.NotFound, "User not found.");
        }

        // Only these three fields are ever written here — Id, Email,
        // PasswordHash, Roles, Status, and ReputationScore are not on
        // UpdateProfileRequest at all, so there is nothing to accidentally
        // copy over even if this method's shape changes later.
        user.FullName = request.FullName.Trim();
        user.Phone = NormalizeOptionalText(request.Phone);
        user.AvatarUrl = NormalizeOptionalText(request.AvatarUrl);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} updated their profile.", userId);

        return ServiceResult<UserProfileResponse>.Success(user.ToProfileResponse());
    }

    public async Task<ServiceResult<PublicUserResponse>> GetPublicProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<PublicUserResponse>.Fail(ServiceErrorCode.NotFound, "User not found.");
        }

        return ServiceResult<PublicUserResponse>.Success(user.ToPublicResponse());
    }

    public async Task<ServiceResult<AdminUserStatusResponse>> UpdateStatusAsync(
        Guid userId,
        UserStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<AdminUserStatusResponse>.Fail(ServiceErrorCode.NotFound, "User not found.");
        }

        user.Status = newStatus;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} status changed to {Status}.", userId, newStatus);

        return ServiceResult<AdminUserStatusResponse>.Success(new AdminUserStatusResponse
        {
            Id = user.Id,
            Status = user.Status
        });
    }

    /// <summary>Treats whitespace-only input as "clear this field" (null).</summary>
    private static string? NormalizeOptionalText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
