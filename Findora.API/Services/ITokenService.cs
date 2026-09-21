using Findora.API.Models;

namespace Findora.API.Services;

public record AccessToken(string Value, DateTime ExpiresAt);

/// <summary>Generates the JWT access token used to authenticate API requests.</summary>
public interface ITokenService
{
    AccessToken GenerateAccessToken(User user, IEnumerable<string> roles);
}
