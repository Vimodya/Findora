using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Findora.API.Data;
using Findora.API.DTOs.Auth;
using Findora.API.DTOs.FoundItems;
using Findora.API.DTOs.ItemCategories;
using Findora.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Findora.API.Tests;

/// <summary>
/// End-to-end tests for Module 5 (Found Item Reporting) against the real
/// HTTP pipeline — mirrors <c>LostItemsEndpointsTests</c>' approach and
/// coverage exactly, adjusted for the found-item field names/route.
/// </summary>
public class FoundItemsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "P@ssword123";

    // Same rationale as LostItemsEndpointsTests — the server serializes
    // enums as strings (Program.cs's JsonStringEnumConverter), so the test
    // client needs the matching options to deserialize them.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FoundItemsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WhenAuthenticated_ReturnsCreatedWithReport()
    {
        var (accessToken, userId, _) = await RegisterAndLoginAsync();
        var categoryId = await GetFirstCategoryIdAsync();
        using var authedClient = AuthedClient(accessToken);

        var response = await authedClient.PostAsJsonAsync("/api/v1/found-items", ValidCreatePayload(categoryId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<FoundItemResponse>(JsonOptions);
        Assert.NotNull(report);
        Assert.Equal(userId, report!.UserId);
        Assert.Equal(categoryId, report.CategoryId);
        Assert.Equal(FoundItemStatus.Active, report.Status);
        Assert.NotEqual(Guid.Empty, report.Id);
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var categoryId = await GetFirstCategoryIdAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/found-items", ValidCreatePayload(categoryId));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidCategory_ReturnsBadRequest()
    {
        var (accessToken, _, _) = await RegisterAndLoginAsync();
        using var authedClient = AuthedClient(accessToken);

        var response = await authedClient.PostAsJsonAsync("/api/v1/found-items", ValidCreatePayload(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithMissingRequiredFields_ReturnsBadRequest()
    {
        var (accessToken, _, _) = await RegisterAndLoginAsync();
        using var authedClient = AuthedClient(accessToken);

        var response = await authedClient.PostAsJsonAsync("/api/v1/found-items", new
        {
            title = "",
            description = "",
            categoryId = Guid.Empty,
            dateFound = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithFutureDateFound_ReturnsBadRequest()
    {
        var (accessToken, _, _) = await RegisterAndLoginAsync();
        var categoryId = await GetFirstCategoryIdAsync();
        using var authedClient = AuthedClient(accessToken);

        var payload = ValidCreatePayload(categoryId);
        payload.DateFound = DateTime.UtcNow.AddDays(5);

        var response = await authedClient.PostAsJsonAsync("/api/v1/found-items", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithOutOfRangeLatitude_ReturnsBadRequest()
    {
        var (accessToken, _, _) = await RegisterAndLoginAsync();
        var categoryId = await GetFirstCategoryIdAsync();
        using var authedClient = AuthedClient(accessToken);

        var payload = ValidCreatePayload(categoryId);
        payload.Latitude = 200;
        payload.Longitude = 0;

        var response = await authedClient.PostAsJsonAsync("/api/v1/found-items", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithPartialCoordinatePair_ReturnsBadRequest()
    {
        var (accessToken, _, _) = await RegisterAndLoginAsync();
        var categoryId = await GetFirstCategoryIdAsync();
        using var authedClient = AuthedClient(accessToken);

        var payload = ValidCreatePayload(categoryId);
        payload.Latitude = 6.9271;
        payload.Longitude = null;

        var response = await authedClient.PostAsJsonAsync("/api/v1/found-items", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ServerAssignsAuthenticatedUsersId_IgnoringClientSuppliedUserId()
    {
        var (accessToken, userId, _) = await RegisterAndLoginAsync();
        var categoryId = await GetFirstCategoryIdAsync();
        using var authedClient = AuthedClient(accessToken);

        // CreateFoundItemRequest has no UserId property at all, so this
        // extra field must simply be ignored — the report must still be
        // owned by the authenticated caller, never the injected id.
        var payload = new Dictionary<string, object?>
        {
            ["title"] = "Found keys",
            ["description"] = "A set of keys with a red keychain",
            ["categoryId"] = categoryId,
            ["dateFound"] = DateTime.UtcNow.AddDays(-1),
            ["userId"] = Guid.NewGuid().ToString()
        };

        var response = await authedClient.PostAsJsonAsync("/api/v1/found-items", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<FoundItemResponse>(JsonOptions);
        Assert.Equal(userId, report!.UserId);
    }

    [Fact]
    public async Task GetById_WhenOwner_ReturnsReport()
    {
        var (accessToken, _, _) = await RegisterAndLoginAsync();
        var categoryId = await GetFirstCategoryIdAsync();
        using var authedClient = AuthedClient(accessToken);

        var created = await CreateReportAsync(authedClient, categoryId);

        var response = await authedClient.GetAsync($"/api/v1/found-items/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<FoundItemResponse>(JsonOptions);
        Assert.Equal(created.Id, report!.Id);
        Assert.Equal(created.Title, report.Title);
    }

    [Fact]
    public async Task GetById_ForNonexistentReport_ReturnsNotFound()
    {
        var (accessToken, _, _) = await RegisterAndLoginAsync();
        using var authedClient = AuthedClient(accessToken);

        var response = await authedClient.GetAsync($"/api/v1/found-items/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ForAnotherUsersReport_ReturnsForbidden()
    {
        var categoryId = await GetFirstCategoryIdAsync();
        var (ownerToken, _, _) = await RegisterAndLoginAsync();
        using var ownerClient = AuthedClient(ownerToken);
        var created = await CreateReportAsync(ownerClient, categoryId);

        var (otherToken, _, _) = await RegisterAndLoginAsync();
        using var otherClient = AuthedClient(otherToken);

        var response = await otherClient.GetAsync($"/api/v1/found-items/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMy_ReturnsOnlyOwnReports_Paginated()
    {
        var categoryId = await GetFirstCategoryIdAsync();
        var (tokenA, _, _) = await RegisterAndLoginAsync();
        using var clientA = AuthedClient(tokenA);
        await CreateReportAsync(clientA, categoryId);
        await CreateReportAsync(clientA, categoryId);

        var (tokenB, _, _) = await RegisterAndLoginAsync();
        using var clientB = AuthedClient(tokenB);
        await CreateReportAsync(clientB, categoryId);

        var response = await clientA.GetAsync("/api/v1/found-items/my?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PaginatedFoundItemsResponse>(JsonOptions);
        Assert.NotNull(page);
        Assert.Equal(2, page!.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(2, page.TotalPages);
    }

    [Fact]
    public async Task Update_WhenOwner_PersistsAllowedFields()
    {
        var categoryId = await GetFirstCategoryIdAsync();
        var (accessToken, _, _) = await RegisterAndLoginAsync();
        using var authedClient = AuthedClient(accessToken);
        var created = await CreateReportAsync(authedClient, categoryId);

        var response = await authedClient.PutAsJsonAsync($"/api/v1/found-items/{created.Id}", new
        {
            title = "Updated title",
            description = "Updated description",
            categoryId,
            dateFound = DateTime.UtcNow.AddDays(-2),
            brand = "Samsung",
            color = "Black"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<FoundItemResponse>(JsonOptions);
        Assert.Equal("Updated title", updated!.Title);
        Assert.Equal("Samsung", updated.Brand);
    }

    [Fact]
    public async Task Update_CannotModifyProtectedFields()
    {
        var categoryId = await GetFirstCategoryIdAsync();
        var (accessToken, userId, _) = await RegisterAndLoginAsync();
        using var authedClient = AuthedClient(accessToken);
        var created = await CreateReportAsync(authedClient, categoryId);

        // UpdateFoundItemRequest has no Id/UserId/CreatedAt/UpdatedAt/
        // IsDeleted/Status property at all, so these must be no-ops.
        var payload = new Dictionary<string, object?>
        {
            ["title"] = "Still my title",
            ["description"] = created.Description,
            ["categoryId"] = categoryId,
            ["dateFound"] = created.DateFound,
            ["id"] = Guid.NewGuid().ToString(),
            ["userId"] = Guid.NewGuid().ToString(),
            ["status"] = "Returned"
        };

        var response = await authedClient.PutAsJsonAsync($"/api/v1/found-items/{created.Id}", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<FoundItemResponse>(JsonOptions);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal(userId, updated.UserId);
        Assert.Equal(FoundItemStatus.Active, updated.Status);
    }

    [Fact]
    public async Task Update_ForAnotherUsersReport_ReturnsForbidden()
    {
        var categoryId = await GetFirstCategoryIdAsync();
        var (ownerToken, _, _) = await RegisterAndLoginAsync();
        using var ownerClient = AuthedClient(ownerToken);
        var created = await CreateReportAsync(ownerClient, categoryId);

        var (otherToken, _, _) = await RegisterAndLoginAsync();
        using var otherClient = AuthedClient(otherToken);

        var response = await otherClient.PutAsJsonAsync($"/api/v1/found-items/{created.Id}", new
        {
            title = "Hijacked",
            description = created.Description,
            categoryId,
            dateFound = created.DateFound
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_WhenOwner_SoftCancelsReport()
    {
        var categoryId = await GetFirstCategoryIdAsync();
        var (accessToken, _, _) = await RegisterAndLoginAsync();
        using var authedClient = AuthedClient(accessToken);
        var created = await CreateReportAsync(authedClient, categoryId);

        var response = await authedClient.DeleteAsync($"/api/v1/found-items/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cancelled = await response.Content.ReadFromJsonAsync<FoundItemResponse>(JsonOptions);
        Assert.Equal(FoundItemStatus.Cancelled, cancelled!.Status);

        // Soft-deleted, not physically removed: it disappears from the
        // owner-visible surface (repository filters !IsDeleted) but the
        // row is never dropped from the database.
        var refetch = await authedClient.GetAsync($"/api/v1/found-items/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, refetch.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FindoraDbContext>();
        var row = await db.FoundItemReports.FirstOrDefaultAsync(r => r.Id == created.Id);
        Assert.NotNull(row);
        Assert.True(row!.IsDeleted);
        Assert.Equal(FoundItemStatus.Cancelled, row.Status);
    }

    [Fact]
    public async Task Cancel_ForAnotherUsersReport_ReturnsForbidden()
    {
        var categoryId = await GetFirstCategoryIdAsync();
        var (ownerToken, _, _) = await RegisterAndLoginAsync();
        using var ownerClient = AuthedClient(ownerToken);
        var created = await CreateReportAsync(ownerClient, categoryId);

        var (otherToken, _, _) = await RegisterAndLoginAsync();
        using var otherClient = AuthedClient(otherToken);

        var response = await otherClient.DeleteAsync($"/api/v1/found-items/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SuspendedUser_CannotCreateOrAccessFoundReports()
    {
        var categoryId = await GetFirstCategoryIdAsync();
        var (userToken, userId, userEmail) = await RegisterAndLoginAsync();
        using var userClient = AuthedClient(userToken);
        var created = await CreateReportAsync(userClient, categoryId);

        await SuspendUserAsync(userId);

        // The already-issued access token stops working immediately,
        // consistent with Module 3's ActiveAccountRequirement enforcement.
        var createAttempt = await userClient.PostAsJsonAsync("/api/v1/found-items", ValidCreatePayload(categoryId));
        Assert.Equal(HttpStatusCode.Forbidden, createAttempt.StatusCode);

        var getAttempt = await userClient.GetAsync($"/api/v1/found-items/{created.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, getAttempt.StatusCode);
    }

    [Fact]
    public async Task ItemCategories_AreSharedWithLostItemReporting()
    {
        var categoryId = await GetFirstCategoryIdAsync();
        var (lostToken, _, _) = await RegisterAndLoginAsync();
        using var lostClient = AuthedClient(lostToken);

        // The exact same seeded category id used by a found report must
        // also be accepted by the Module 4 lost-item endpoint — proving
        // both modules share one ItemCategories table rather than each
        // having their own.
        var lostResponse = await lostClient.PostAsJsonAsync("/api/v1/lost-items", new
        {
            title = "Lost phone",
            description = "Black phone",
            categoryId,
            dateLost = DateTime.UtcNow.AddDays(-1)
        });

        Assert.Equal(HttpStatusCode.Created, lostResponse.StatusCode);
    }

    [Fact]
    public async Task GetItemCategories_ReturnsSeededCategories_WithoutAuthentication()
    {
        var response = await _client.GetAsync("/api/v1/item-categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<ItemCategoryResponse>>(JsonOptions);
        Assert.NotNull(categories);
        Assert.True(categories!.Count >= 10);
        Assert.Contains(categories, c => c.Name == "Electronics");
        Assert.Contains(categories, c => c.Name == "Other");
    }

    private async Task<FoundItemResponse> CreateReportAsync(HttpClient client, Guid categoryId)
    {
        var response = await client.PostAsJsonAsync("/api/v1/found-items", ValidCreatePayload(categoryId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FoundItemResponse>(JsonOptions))!;
    }

    private static CreatePayload ValidCreatePayload(Guid categoryId) => new()
    {
        Title = "Found backpack",
        Description = "Navy blue backpack left on a bench.",
        CategoryId = categoryId,
        DateFound = DateTime.UtcNow.AddDays(-1),
        Brand = "Northface",
        Color = "Navy"
    };

    private async Task<Guid> GetFirstCategoryIdAsync()
    {
        var response = await _client.GetAsync("/api/v1/item-categories");
        response.EnsureSuccessStatusCode();
        var categories = await response.Content.ReadFromJsonAsync<List<ItemCategoryResponse>>(JsonOptions);
        return categories![0].Id;
    }

    private HttpClient AuthedClient(string accessToken)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private async Task<(string AccessToken, Guid UserId, string Email)> RegisterAndLoginAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@example.test";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = Password, fullName = "Test User" });
        registerResponse.EnsureSuccessStatusCode();
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        var userId = registered!.User.Id;

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        return (auth!.AccessToken, userId, email);
    }

    /// <summary>Mirrors <c>LostItemsEndpointsTests.SuspendUserAsync</c> — reaches into the test database directly since there's no self-service way to suspend an account.</summary>
    private async Task SuspendUserAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FindoraDbContext>();

        var user = await db.Users.SingleAsync(u => u.Id == userId);
        user.Status = UserStatus.Suspended;
        await db.SaveChangesAsync();
    }

    /// <summary>Mutable POCO (rather than an anonymous type) so individual field overrides read cleanly in the invalid-input tests.</summary>
    private class CreatePayload
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public DateTime DateFound { get; set; }
        public string? Brand { get; set; }
        public string? Color { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
