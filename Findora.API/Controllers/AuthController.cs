using Findora.API.DTOs.Auth;
using Findora.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Findora.API.Controllers;

/// <summary>
/// Module 2 — Authentication &amp; Authorization. Deliberately thin: every
/// action just validates the model (handled automatically by
/// <c>[ApiController]</c>), delegates to <see cref="IAuthService"/>, and
/// maps the result to an HTTP response. No business logic lives here.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return result.ErrorCode == AuthErrorCode.DuplicateEmail
                ? Conflict(new MessageResponse(result.ErrorMessage!))
                : BadRequest(new MessageResponse(result.ErrorMessage!));
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, GetClientIp(), cancellationToken);

        if (!result.Succeeded)
        {
            // Credentials were valid but the account isn't active (Module
            // 3) — 403, distinct from 401 for actually-wrong credentials.
            return result.ErrorCode == AuthErrorCode.AccountNotActive
                ? StatusCode(StatusCodes.Status403Forbidden, new MessageResponse(result.ErrorMessage!))
                : Unauthorized(new MessageResponse(result.ErrorMessage!));
        }

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request, GetClientIp(), cancellationToken);

        if (!result.Succeeded)
        {
            return result.ErrorCode == AuthErrorCode.AccountNotActive
                ? StatusCode(StatusCodes.Status403Forbidden, new MessageResponse(result.ErrorMessage!))
                : Unauthorized(new MessageResponse(result.ErrorMessage!));
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Requires a valid access token — logging out is an authenticated
    /// action. The refresh token to revoke is supplied in the body (an
    /// access token has no notion of "which session"; a refresh token does).
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LogoutAsync(request, cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.VerifyEmailAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new MessageResponse(result.ErrorMessage!));
        }

        return Ok(result.Value);
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ResendVerificationAsync(request, cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new MessageResponse(result.ErrorMessage!));
        }

        return Ok(result.Value);
    }

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
