using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Findora.API.DTOs.Auth;

namespace Findora.API.Tests;

/// <summary>
/// End-to-end tests against the real HTTP pipeline (JWT auth, rate
/// limiting, exception handling, controllers — all of it) with only the
/// database and email sender swapped for test doubles. Covers the flows
/// Module 2 explicitly calls out for testing.
/// </summary>
public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "P@ssword123";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreatedWithUnverifiedUser()
    {
        var email = UniqueEmail();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = Password, fullName = "Test User" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(body);
        Assert.Equal(email, body!.User.Email, ignoreCase: true);
        Assert.False(body.User.IsEmailVerified);
        Assert.Contains("User", body.User.Roles);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = UniqueEmail();
        var payload = new { email, password = Password, fullName = "Test User" };

        var first = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidPayload_ReturnsValidationError()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "not-an-email", password = "short", fullName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var email = await RegisterUserAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.True(body.AccessTokenExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = await RegisterUserAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "WrongPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = UniqueEmail(), password = "WhateverPassword1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithNoBearerToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = "irrelevant" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidBearerToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "this.is.not.a.valid.jwt");

        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = "irrelevant" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidAccessToken_RevokesTheRefreshToken()
    {
        var email = await RegisterUserAsync();
        var login = await LoginAsync(email);

        using var authedClient = _factory.CreateClient();
        authedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var logoutResponse = await authedClient.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var refreshAfterLogout = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidToken_RotatesTokenAndRejectsReuse()
    {
        var email = await RegisterUserAsync();
        var login = await LoginAsync(email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rotated = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(rotated);
        Assert.NotEqual(login.RefreshToken, rotated!.RefreshToken);

        // Reusing the now-rotated-away token must fail...
        var reuse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        // ...and (reuse-detection) revokes the token it was rotated into as well.
        var afterReuseDetection = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = rotated.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuseDetection.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "totally-made-up-token" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_WithValidToken_MarksUserVerifiedAndCannotBeReused()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = Password, fullName = "Test User" });

        var sent = _factory.EmailSender.SentEmails.Last(e => e.To == email && e.Subject.Contains("Verify"));
        var token = FakeEmailSender.ExtractToken(sent);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var reuse = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token });
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new { token = "not-a-real-token" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_ReturnsIdenticalGenericResponse_RegardlessOfWhetherEmailExists()
    {
        var unknownResponse = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = UniqueEmail() });
        var knownEmail = await RegisterUserAsync();
        var knownResponse = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = knownEmail });

        Assert.Equal(HttpStatusCode.OK, unknownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, knownResponse.StatusCode);

        var unknownBody = await unknownResponse.Content.ReadFromJsonAsync<MessageResponse>();
        var knownBody = await knownResponse.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.Equal(unknownBody!.Message, knownBody!.Message);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ChangesPasswordAndRevokesExistingSessions()
    {
        var email = await RegisterUserAsync();
        var login = await LoginAsync(email);

        await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        var sent = _factory.EmailSender.SentEmails.Last(e => e.To == email && e.Subject.Contains("Reset"));
        var token = FakeEmailSender.ExtractToken(sent);

        const string newPassword = "NewP@ssword456";
        var resetResponse = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new { token, newPassword });
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        // The refresh token issued before the reset is now dead.
        var refreshAfterReset = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterReset.StatusCode);

        // Old password rejected, new password works.
        var oldLogin = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { token = "not-a-real-token", newPassword = "SomeNewP@ss1" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.test";

    private async Task<string> RegisterUserAsync()
    {
        var email = UniqueEmail();
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = Password, fullName = "Test User" });
        response.EnsureSuccessStatusCode();
        return email;
    }

    private async Task<AuthResponse> LoginAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
}
