using Findora.API.DTOs.Auth;

namespace Findora.API.Services;

/// <summary>
/// All authentication business logic (Module 2). Kept out of the controller
/// so <c>AuthController</c> stays thin — it only binds/validates the
/// request, calls the matching method here, and maps the result to an HTTP
/// response.
/// </summary>
public interface IAuthService
{
    Task<AuthResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<AuthResult<AuthResponse>> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<AuthResult<MessageResponse>> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult<MessageResponse>> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult<MessageResponse>> ResendVerificationAsync(ResendVerificationRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult<MessageResponse>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult<MessageResponse>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
