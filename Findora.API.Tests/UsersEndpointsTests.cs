using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Findora.API.Data;
using Findora.API.DTOs.Auth;
using Findora.API.DTOs.Users;
using Findora.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Findora.API.Tests;

/// <summary>
/// End-to-end tests for Module 3 (User &amp; Profile Management) against the
/// real HTTP pipeline — same approach as <c>AuthEndpointsTests</c>. Uses the
/// real register/login endpoints to get valid tokens, then promotes a user
/// to Admin directly via the test database (there is no self-service way to
/// become an Admin, by design).
/// </summary>
public class UsersEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "P@ssword123";

    // The server serializes enums (e.g. UserStatus) as strings via the
    // JsonStringEnumConverter registered in Program.cs — System.Net.Http.Json's
    // default deserialization options don't know about that on the test
    // (client) side unless told explicitly, so responses containing an enum
    // must be read with this.
    //
    // Built from JsonSerializerDefaults.Web (not a bare `new()`) so it keeps
    // the camelCase-aware, case-insensitive property matching that
    // ReadFromJsonAsync<T>()'s parameterless overload uses internally —
    // without this, every other property (Email, FullName, Id, ...) fails
    // to bind against the server's camelCase JSON and silently defaults.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UsersEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOwnProfile_WhenAuthenticated_ReturnsProfile()
    {
        var (accessToken, _, email) = await RegisterAndLoginAsync();

        using var authedClient = AuthedClient(accessToken);
        var response = await authedClient.GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal(email, profile!.Email, ignoreCase: true);
        Assert.Equal(UserStatus.Active, profile.Status);
        Assert.Equal(0, profile.ReputationScore);
        Assert.Contains("User", profile.Roles);
    }

    [Fact]
    public async Task GetOwnProfile_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnProfile_WithAllowedFields_PersistsChanges()
    {
        var (accessToken, _, _) = await RegisterAndLoginAsync();
        using var authedClient = AuthedClient(accessToken);

        var response = await authedClient.PutAsJsonAsync("/api/v1/users/me", new
        {
            fullName = "Updated Name",
            phone = "+1 555 0100",
            avatarUrl = "https://example.test/avatar.png"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>(JsonOptions);
        Assert.NotNull(profile);
        Assert.Equal("Updated Name", profile!.FullName);
        Assert.Equal("+1 555 0100", profile.Phone);
        Assert.Equal("https://example.test/avatar.png", profile.AvatarUrl);

        // Re-fetch to confirm it was actually persisted, not just echoed back.
        var refetched = await (await authedClient.GetAsync("/api/v1/users/me")).Content.ReadFromJsonAsync<UserProfileResponse>(JsonOptions);
        Assert.Equal("Updated Name", refetched!.FullName);
    }

    [Fact]
    public async Task UpdateOwnProfile_CannotModifyProtectedFields()
    {
        var (accessToken, userId, _) = await RegisterAndLoginAsync();
        using var authedClient = AuthedClient(accessToken);

        // UpdateProfileRequest has no Id/Email/Status/ReputationScore/Roles
        // property at all, so sending them is a no-op regardless of what
        // System.Text.Json does with unknown properties — this asserts the
        // outcome, not the mechanism.
        var payload = new Dictionary<string, object?>
        {
            ["fullName"] = "Still Me",
            ["email"] = "hijacked@example.test",
            ["status"] = "Admin",
            ["reputationScore"] = 9999,
            ["id"] = Guid.NewGuid().ToString()
        };

        var response = await authedClient.PutAsJsonAsync("/api/v1/users/me", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<UserProfileResponse>(JsonOptions);
        Assert.Equal(userId, profile!.Id);
        Assert.NotEqual("hijacked@example.test", profile.Email);
        Assert.Equal(UserStatus.Active, profile.Status);
        Assert.Equal(0, profile.ReputationScore);
    }

    [Fact]
    public async Task UpdateOwnProfile_NeverAffectsAnotherUsersProfile()
    {
        var (accessTokenA, _, _) = await RegisterAndLoginAsync();
        var (_, userIdB, _) = await RegisterAndLoginAsync();

        using var clientA = AuthedClient(accessTokenA);
        await clientA.PutAsJsonAsync("/api/v1/users/me", new { fullName = "User A's New Name" });

        var publicB = await (await _client.GetAsync($"/api/v1/users/{userIdB}")).Content.ReadFromJsonAsync<PublicUserResponse>();
        Assert.NotEqual("User A's New Name", publicB!.FullName);
    }

    [Fact]
    public async Task GetPublicProfile_DoesNotExposeSensitiveInformation()
    {
        var (_, userId, email) = await RegisterAndLoginAsync();

        var response = await _client.GetAsync($"/api/v1/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(email, raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reputationScore", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("status", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", raw, StringComparison.OrdinalIgnoreCase);

        var publicProfile = await response.Content.ReadFromJsonAsync<PublicUserResponse>();
        Assert.Equal(userId, publicProfile!.Id);
    }

    [Fact]
    public async Task GetPublicProfile_ForUnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminCanChangeAccountStatus()
    {
        var (adminToken, _, _) = await RegisterAndLoginAsync(promoteToAdmin: true);
        var (_, targetUserId, _) = await RegisterAndLoginAsync();

        using var adminClient = AuthedClient(adminToken);
        var response = await adminClient.PatchAsJsonAsync($"/api/v1/users/{targetUserId}/status", new { status = "Suspended" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserStatusResponse>(JsonOptions);
        Assert.Equal(UserStatus.Suspended, body!.Status);
    }

    [Fact]
    public async Task NormalUserCannotChangeAccountStatus()
    {
        var (userToken, _, _) = await RegisterAndLoginAsync();
        var (_, targetUserId, _) = await RegisterAndLoginAsync();

        using var userClient = AuthedClient(userToken);
        var response = await userClient.PatchAsJsonAsync($"/api/v1/users/{targetUserId}/status", new { status = "Suspended" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NormalUserCannotChangeTheirOwnStatus()
    {
        var (userToken, userId, _) = await RegisterAndLoginAsync();

        using var userClient = AuthedClient(userToken);
        var response = await userClient.PatchAsJsonAsync($"/api/v1/users/{userId}/status", new { status = "Admin" });

        // Not even a valid status value ("Admin" isn't one), but the role
        // check must reject this before that's ever evaluated.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task StatusUpdate_WithInvalidStatusValue_ReturnsBadRequest()
    {
        var (adminToken, _, _) = await RegisterAndLoginAsync(promoteToAdmin: true);
        var (_, targetUserId, _) = await RegisterAndLoginAsync();

        using var adminClient = AuthedClient(adminToken);
        var response = await adminClient.PatchAsJsonAsync($"/api/v1/users/{targetUserId}/status", new { status = "NotARealStatus" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SuspendedAccount_CannotPerformProtectedActions()
    {
        var (adminToken, _, _) = await RegisterAndLoginAsync(promoteToAdmin: true);
        var (userToken, targetUserId, targetEmail) = await RegisterAndLoginAsync();

        using var adminClient = AuthedClient(adminToken);
        var suspend = await adminClient.PatchAsJsonAsync($"/api/v1/users/{targetUserId}/status", new { status = "Suspended" });
        Assert.Equal(HttpStatusCode.OK, suspend.StatusCode);

        // The already-issued access token stops working immediately —
        // no need to wait for it to expire or for the user to log in again.
        using var suspendedClient = AuthedClient(userToken);
        var protectedCall = await suspendedClient.GetAsync("/api/v1/users/me");
        Assert.Equal(HttpStatusCode.Forbidden, protectedCall.StatusCode);

        // And they can no longer authenticate at all.
        var loginAttempt = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = targetEmail, password = Password });
        Assert.Equal(HttpStatusCode.Forbidden, loginAttempt.StatusCode);
    }

    [Fact]
    public async Task DeactivatedAccount_CannotPerformProtectedActions()
    {
        var (adminToken, _, _) = await RegisterAndLoginAsync(promoteToAdmin: true);
        var (userToken, targetUserId, _) = await RegisterAndLoginAsync();

        using var adminClient = AuthedClient(adminToken);
        await adminClient.PatchAsJsonAsync($"/api/v1/users/{targetUserId}/status", new { status = "Deactivated" });

        using var deactivatedClient = AuthedClient(userToken);
        var response = await deactivatedClient.GetAsync("/api/v1/users/me");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient AuthedClient(string accessToken)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private async Task<(string AccessToken, Guid UserId, string Email)> RegisterAndLoginAsync(bool promoteToAdmin = false)
    {
        var email = $"user-{Guid.NewGuid():N}@example.test";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = Password, fullName = "Test User" });
        registerResponse.EnsureSuccessStatusCode();
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        var userId = registered!.User.Id;

        if (promoteToAdmin)
        {
            await PromoteToAdminAsync(userId);
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        return (auth!.AccessToken, userId, email);
    }

    /// <summary>
    /// There is no self-service way to become an Admin (by design) — tests
    /// that need one reach into the test database directly, mirroring how
    /// a real Admin account would be seeded/promoted out-of-band.
    /// </summary>
    private async Task PromoteToAdminAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FindoraDbContext>();

        var adminRole = await db.Roles.SingleAsync(r => r.Name == RoleNames.Admin);
        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = adminRole.Id });
        await db.SaveChangesAsync();
    }
}
