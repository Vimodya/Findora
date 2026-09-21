namespace Findora.API.Services;

/// <summary>
/// Distinguishes expected auth business outcomes (duplicate email, bad
/// credentials, bad token) from unexpected errors — the latter still go
/// through the normal exception path / global exception middleware. Lets
/// <c>AuthController</c> map each outcome to the right HTTP status without
/// using exceptions for ordinary control flow.
/// </summary>
public enum AuthErrorCode
{
    None,
    DuplicateEmail,
    InvalidCredentials,
    InvalidOrExpiredToken,

    /// <summary>
    /// Credentials/token were valid, but the account is not
    /// <see cref="Models.UserStatus.Active"/> (Module 3). Distinct from
    /// <see cref="InvalidCredentials"/> because identity IS confirmed here
    /// — access is what's forbidden, hence a 403 in <c>AuthController</c>,
    /// not a 401.
    /// </summary>
    AccountNotActive
}

/// <summary>Outcome of an <see cref="IAuthService"/> operation.</summary>
public class AuthResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public AuthErrorCode ErrorCode { get; init; } = AuthErrorCode.None;
    public string? ErrorMessage { get; init; }

    public static AuthResult<T> Success(T value) => new() { Succeeded = true, Value = value };

    public static AuthResult<T> Fail(AuthErrorCode code, string message) =>
        new() { Succeeded = false, ErrorCode = code, ErrorMessage = message };
}
