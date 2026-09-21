using Findora.API.Configuration;
using Findora.API.Data;
using Findora.API.DTOs.Auth;
using Findora.API.Mappings;
using Findora.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Findora.API.Services;

public class AuthService : IAuthService
{
    private const int EmailVerificationTokenLifetimeHours = 24;
    private const int PasswordResetTokenLifetimeMinutes = 30;

    // A fixed, never-issued password hash burned for a constant-time-ish
    // comparison when the account doesn't exist, so a login attempt's
    // response time doesn't reveal whether an email is registered.
    private static readonly string DummyPasswordHash =
        new PasswordHasher<User>().HashPassword(new User(), Guid.NewGuid().ToString());

    private readonly FindoraDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        FindoraDbContext db,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        IEmailSender emailSender,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<AuthResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var emailTaken = await _db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
        if (emailTaken)
        {
            return AuthResult<RegisterResponse>.Fail(AuthErrorCode.DuplicateEmail, "An account with this email already exists.");
        }

        var userRole = await _db.Roles.SingleAsync(r => r.Name == RoleNames.User, cancellationToken);

        var user = new User
        {
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            FullName = request.FullName.Trim(),
            IsEmailVerified = false
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        user.UserRoles.Add(new UserRole { User = user, Role = userRole });

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        await IssueEmailVerificationTokenAsync(user, cancellationToken);

        _logger.LogInformation("New user registered: {UserId}", user.Id);

        return AuthResult<RegisterResponse>.Success(new RegisterResponse
        {
            Message = "Registration successful. Please check your email to verify your account.",
            User = user.ToSummaryResponse()
        });
    }

    public async Task<AuthResult<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            // Burn roughly the same time a real verification would take so
            // "unknown email" and "wrong password" aren't distinguishable
            // by response time.
            _ = _passwordHasher.VerifyHashedPassword(new User(), DummyPasswordHash, request.Password);
            _logger.LogInformation("Login attempt for an unknown email.");
            return AuthResult<AuthResponse>.Fail(AuthErrorCode.InvalidCredentials, "Invalid email or password.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            _logger.LogInformation("Failed login attempt for user {UserId}.", user.Id);
            return AuthResult<AuthResponse>.Fail(AuthErrorCode.InvalidCredentials, "Invalid email or password.");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        }

        if (user.Status != UserStatus.Active)
        {
            // Checked only after the password is confirmed correct, so a
            // failed guess never reveals anything about account status.
            _logger.LogInformation("Login rejected for user {UserId}: account status is {Status}.", user.Id, user.Status);
            return AuthResult<AuthResponse>.Fail(AuthErrorCode.AccountNotActive, "This account is not active. Contact support for details.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray();
        var response = await IssueAuthResponseAsync(user, roles, ipAddress, cancellationToken);

        _logger.LogInformation("User {UserId} logged in.", user.Id);

        return AuthResult<AuthResponse>.Success(response);
    }

    public async Task<AuthResult<AuthResponse>> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var tokenHash = OpaqueTokenGenerator.Hash(request.RefreshToken);
        var utcNow = DateTime.UtcNow;

        var existingToken = await _db.RefreshTokens
            .Include(rt => rt.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null)
        {
            return AuthResult<AuthResponse>.Fail(AuthErrorCode.InvalidOrExpiredToken, "Invalid or expired refresh token.");
        }

        if (!existingToken.IsActive(utcNow))
        {
            // Reuse of an already-revoked token is a strong signal it was
            // stolen and replayed after the legitimate rotation — revoke
            // every other active session for this user defensively.
            if (existingToken.RevokedAt is not null)
            {
                await RevokeAllActiveRefreshTokensAsync(existingToken.UserId, utcNow, cancellationToken);
                _logger.LogWarning(
                    "Reuse of a revoked refresh token detected for user {UserId}; all sessions revoked.",
                    existingToken.UserId);
            }

            return AuthResult<AuthResponse>.Fail(AuthErrorCode.InvalidOrExpiredToken, "Invalid or expired refresh token.");
        }

        var user = existingToken.User;

        if (user.Status != UserStatus.Active)
        {
            // A suspended/deactivated account must not be able to mint a
            // fresh access token either, even with an otherwise-valid
            // refresh token (Module 3).
            _logger.LogInformation("Refresh rejected for user {UserId}: account status is {Status}.", user.Id, user.Status);
            return AuthResult<AuthResponse>.Fail(AuthErrorCode.AccountNotActive, "This account is not active. Contact support for details.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray();

        var rawNewRefreshToken = OpaqueTokenGenerator.GenerateRawToken();
        var newTokenHash = OpaqueTokenGenerator.Hash(rawNewRefreshToken);
        var refreshTokenExpiresAt = utcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        existingToken.RevokedAt = utcNow;
        existingToken.ReplacedByTokenHash = newTokenHash;

        _db.RefreshTokens.Add(new RefreshToken
        {
            User = user,
            TokenHash = newTokenHash,
            ExpiresAt = refreshTokenExpiresAt,
            CreatedByIp = ipAddress
        });

        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token rotated for user {UserId}.", user.Id);

        return AuthResult<AuthResponse>.Success(new AuthResponse
        {
            AccessToken = accessToken.Value,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = rawNewRefreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            User = user.ToSummaryResponse()
        });
    }

    public async Task<AuthResult<MessageResponse>> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = OpaqueTokenGenerator.Hash(request.RefreshToken);
        var token = await _db.RefreshTokens.SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (token is not null && token.RevokedAt is null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("User {UserId} logged out.", token.UserId);
        }

        // Always the same response — an unknown/already-revoked token still
        // means the caller ends up logged out.
        return AuthResult<MessageResponse>.Success(new MessageResponse("Logged out."));
    }

    public async Task<AuthResult<MessageResponse>> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = OpaqueTokenGenerator.Hash(request.Token);
        var utcNow = DateTime.UtcNow;

        var verification = await _db.EmailVerificationTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (verification is null || !verification.IsValid(utcNow))
        {
            return AuthResult<MessageResponse>.Fail(AuthErrorCode.InvalidOrExpiredToken, "Invalid or expired verification token.");
        }

        verification.UsedAt = utcNow;
        verification.User.IsEmailVerified = true;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email verified for user {UserId}.", verification.UserId);

        return AuthResult<MessageResponse>.Success(new MessageResponse("Email verified successfully."));
    }

    public async Task<AuthResult<MessageResponse>> ResendVerificationAsync(ResendVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await _db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && !u.IsDeleted, cancellationToken);

        if (user is not null && !user.IsEmailVerified)
        {
            await IssueEmailVerificationTokenAsync(user, cancellationToken);
        }

        // Same email-enumeration protection as forgot-password: identical
        // response whether the account exists, is already verified, or not.
        return AuthResult<MessageResponse>.Success(
            new MessageResponse("If that email exists and isn't verified yet, a new verification link has been sent."));
    }

    public async Task<AuthResult<MessageResponse>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await _db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && !u.IsDeleted, cancellationToken);

        if (user is not null)
        {
            var rawToken = OpaqueTokenGenerator.GenerateRawToken();
            _db.PasswordResetTokens.Add(new PasswordResetToken
            {
                User = user,
                TokenHash = OpaqueTokenGenerator.Hash(rawToken),
                ExpiresAt = DateTime.UtcNow.AddMinutes(PasswordResetTokenLifetimeMinutes)
            });
            await _db.SaveChangesAsync(cancellationToken);

            var body =
                $"Use this token to reset your Findora password:\n\n{rawToken}\n\n" +
                $"This token expires in {PasswordResetTokenLifetimeMinutes} minutes. " +
                "If you didn't request this, you can safely ignore this email.";
            await _emailSender.SendAsync(user.Email, "Reset your Findora password", body, cancellationToken);

            _logger.LogInformation("Password reset requested for user {UserId}.", user.Id);
        }

        // Never reveal whether the email exists — identical response either way.
        return AuthResult<MessageResponse>.Success(
            new MessageResponse("If an account with that email exists, a password reset link has been sent."));
    }

    public async Task<AuthResult<MessageResponse>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = OpaqueTokenGenerator.Hash(request.Token);
        var utcNow = DateTime.UtcNow;

        var resetToken = await _db.PasswordResetTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (resetToken is null || !resetToken.IsValid(utcNow))
        {
            return AuthResult<MessageResponse>.Fail(AuthErrorCode.InvalidOrExpiredToken, "Invalid or expired reset token.");
        }

        var user = resetToken.User;
        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        resetToken.UsedAt = utcNow;
        await _db.SaveChangesAsync(cancellationToken);

        // A password reset ends every other active session — a reasonable,
        // low-complexity security measure for this project's scope.
        await RevokeAllActiveRefreshTokensAsync(user.Id, utcNow, cancellationToken);

        _logger.LogInformation("Password reset completed for user {UserId}.", user.Id);

        return AuthResult<MessageResponse>.Success(new MessageResponse("Password has been reset. Please log in again."));
    }

    private async Task<AuthResponse> IssueAuthResponseAsync(
        User user,
        IReadOnlyCollection<string> roles,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.GenerateAccessToken(user, roles);

        var rawRefreshToken = OpaqueTokenGenerator.GenerateRawToken();
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            User = user,
            TokenHash = OpaqueTokenGenerator.Hash(rawRefreshToken),
            ExpiresAt = refreshTokenExpiresAt,
            CreatedByIp = ipAddress
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken.Value,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            User = user.ToSummaryResponse()
        };
    }

    private async Task IssueEmailVerificationTokenAsync(User user, CancellationToken cancellationToken)
    {
        var rawToken = OpaqueTokenGenerator.GenerateRawToken();
        _db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            User = user,
            TokenHash = OpaqueTokenGenerator.Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.AddHours(EmailVerificationTokenLifetimeHours)
        });
        await _db.SaveChangesAsync(cancellationToken);

        var body =
            $"Welcome to Findora! Use this token to verify your email:\n\n{rawToken}\n\n" +
            $"This token expires in {EmailVerificationTokenLifetimeHours} hours.";
        await _emailSender.SendAsync(user.Email, "Verify your Findora email", body, cancellationToken);
    }

    private async Task RevokeAllActiveRefreshTokensAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        if (activeTokens.Count == 0)
        {
            return;
        }

        foreach (var token in activeTokens)
        {
            token.RevokedAt = utcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
