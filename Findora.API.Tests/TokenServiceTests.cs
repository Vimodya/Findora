using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Findora.API.Configuration;
using Findora.API.Models;
using Findora.API.Services;
using Microsoft.Extensions.Options;

namespace Findora.API.Tests;

public class TokenServiceTests
{
    private static ITokenService CreateService(int accessTokenExpirationMinutes = 15)
    {
        var settings = new JwtSettings
        {
            Issuer = "Findora.Tests",
            Audience = "Findora.Tests.Client",
            // Fixed, clearly-fake unit-test-only key.
            SigningKey = "unit-test-signing-key-not-for-production-use-0123456789",
            AccessTokenExpirationMinutes = accessTokenExpirationMinutes
        };

        return new TokenService(Options.Create(settings));
    }

    [Fact]
    public void GenerateAccessToken_IncludesExpectedClaims()
    {
        var service = CreateService();
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.test", FullName = "Test User" };

        var token = service.GenerateAccessToken(user, new[] { RoleNames.User, RoleNames.Admin });
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Name && c.Value == user.FullName);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == RoleNames.User);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == RoleNames.Admin);
        Assert.Equal("Findora.Tests", jwt.Issuer);
        Assert.Contains("Findora.Tests.Client", jwt.Audiences);
    }

    [Fact]
    public void GenerateAccessToken_SetsExpirationConsistentWithConfiguration()
    {
        var service = CreateService(accessTokenExpirationMinutes: 15);
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.test", FullName = "Test User" };

        var before = DateTime.UtcNow;
        var token = service.GenerateAccessToken(user, Array.Empty<string>());
        var after = DateTime.UtcNow;

        Assert.True(token.ExpiresAt >= before.AddMinutes(15));
        Assert.True(token.ExpiresAt <= after.AddMinutes(15).AddSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_ProducesADifferentJtiEachCall()
    {
        var service = CreateService();
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.test", FullName = "Test User" };

        var first = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateAccessToken(user, Array.Empty<string>()).Value);
        var second = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateAccessToken(user, Array.Empty<string>()).Value);

        var firstJti = first.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var secondJti = second.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(firstJti, secondJti);
    }
}
