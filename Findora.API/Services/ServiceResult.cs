namespace Findora.API.Services;

/// <summary>
/// General-purpose outcome codes for services outside the auth surface
/// (which has its own <see cref="AuthErrorCode"/>/<see cref="AuthResult{T}"/>,
/// left as-is). Lets a thin controller map a service outcome to the right
/// HTTP status without exceptions for ordinary control flow.
/// </summary>
public enum ServiceErrorCode
{
    None,
    NotFound
}

/// <summary>Outcome of a non-auth service operation (e.g. <see cref="IUserProfileService"/>).</summary>
public class ServiceResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public ServiceErrorCode ErrorCode { get; init; } = ServiceErrorCode.None;
    public string? ErrorMessage { get; init; }

    public static ServiceResult<T> Success(T value) => new() { Succeeded = true, Value = value };

    public static ServiceResult<T> Fail(ServiceErrorCode code, string message) =>
        new() { Succeeded = false, ErrorCode = code, ErrorMessage = message };
}
