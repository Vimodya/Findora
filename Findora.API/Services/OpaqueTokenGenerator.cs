using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Findora.API.Services;

/// <summary>
/// Generates and hashes the opaque, single-use tokens used for refresh
/// tokens, email verification, and password reset. The raw token is only
/// ever handed to the client (or, for Module 2's email stub, logged as part
/// of the "sent" message) — only its SHA-256 hash is persisted, so a
/// database leak does not expose usable tokens.
/// </summary>
public static class OpaqueTokenGenerator
{
    /// <summary>Generates a cryptographically random, URL-safe opaque token.</summary>
    public static string GenerateRawToken(int byteLength = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    /// <summary>Computes the SHA-256 hash (hex string) of a raw token, for storage/lookup.</summary>
    public static string Hash(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
