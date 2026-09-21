using System.Security.Claims;
using Findora.API.DTOs.Auth;
using Findora.API.DTOs.Users;
using Findora.API.Models;
using Findora.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Findora.API.Controllers;

/// <summary>
/// Module 3 — User &amp; Profile Management. Thin, like <c>AuthController</c>:
/// binds/validates the request, delegates to <see cref="IUserProfileService"/>,
/// maps the result to an HTTP response.
///
/// Report-history foundation: once Modules 4/5 introduce
/// <c>LostReport</c>/<c>FoundReport</c> entities, a
/// <c>GET /api/v1/users/me/reports</c>-style endpoint (or equivalent query
/// surface) belongs here, listing the authenticated user's own reports. Not
/// implemented yet — there is nothing to query without those entities, and
/// no placeholder table has been added just to pre-wire this.
/// </summary>
[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    public UsersController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOwnProfile(CancellationToken cancellationToken)
    {
        var result = await _userProfileService.GetOwnProfileAsync(GetCurrentUserId(), cancellationToken);

        if (!result.Succeeded)
        {
            return NotFound(new MessageResponse(result.ErrorMessage!));
        }

        return Ok(result.Value);
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateOwnProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _userProfileService.UpdateOwnProfileAsync(GetCurrentUserId(), request, cancellationToken);

        if (!result.Succeeded)
        {
            return NotFound(new MessageResponse(result.ErrorMessage!));
        }

        return Ok(result.Value);
    }

    /// <summary>Limited public view — no authentication required.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicProfile(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userProfileService.GetPublicProfileAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            return NotFound(new MessageResponse(result.ErrorMessage!));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Admin-only. <c>[Authorize(Roles = ...)]</c> builds on the default
    /// authorization policy, so this also picks up the
    /// <c>ActiveAccountRequirement</c> — a suspended/deactivated Admin
    /// account cannot use this endpoint either.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = RoleNames.Admin)]
    [ProducesResponseType(typeof(AdminUserStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] AdminUpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _userProfileService.UpdateStatusAsync(id, request.Status!.Value, cancellationToken);

        if (!result.Succeeded)
        {
            return NotFound(new MessageResponse(result.ErrorMessage!));
        }

        return Ok(result.Value);
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
