using System.Net;
using System.Net.Http.Json;

namespace Findora.API.Tests;

/// <summary>A factory with a deliberately tiny auth rate limit, isolated from the other test classes' shared factory.</summary>
public class RateLimitedWebApplicationFactory : CustomWebApplicationFactory
{
    protected override int AuthPermitLimit => 3;
}

public class RateLimitingTests : IClassFixture<RateLimitedWebApplicationFactory>
{
    private readonly RateLimitedWebApplicationFactory _factory;

    public RateLimitingTests(RateLimitedWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExceedingTheAuthRateLimit_Returns429()
    {
        using var client = _factory.CreateClient();
        var payload = new { email = "rate-limit-test@example.test", password = "WrongPassword1" };

        // The first AuthPermitLimit (3) requests go through the normal auth
        // pipeline (they fail login, but with 401 — not rate-limited yet).
        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", payload);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // The next request in the same window is rejected by the limiter itself.
        var overLimitResponse = await client.PostAsJsonAsync("/api/v1/auth/login", payload);
        Assert.Equal(HttpStatusCode.TooManyRequests, overLimitResponse.StatusCode);
    }
}
