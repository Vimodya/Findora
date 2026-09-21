using System.Security.Claims;
using Findora.API.Data;
using Findora.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Findora.API.Tests;

/// <summary>
/// Tests the actual role-based authorization policies registered in
/// <c>Program.cs</c> (not a hand-rolled copy of them), exercised directly
/// through <see cref="IAuthorizationService"/> — this is what backs every
/// controller's <c>403</c> behavior for a role mismatch. HTTP-level 401
/// (missing/invalid token) is covered in <c>AuthEndpointsTests</c>; this
/// covers the "authenticated but wrong role" (403) half of requirement #9.
///
/// Since Module 3 added <c>ActiveAccountRequirement</c> to these same
/// policies, every principal here needs a matching, genuinely
/// <see cref="UserStatus.Active"/> user row in the test database — a
/// principal with only a role claim and no resolvable/active user can
/// never succeed authorization, by design.
/// </summary>
public class AuthorizationPolicyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationPolicyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminOnlyPolicy_RejectsPlainUserRole()
    {
        var result = await AuthorizeAsActiveUserAsync("AdminOnly", RoleNames.User);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AdminOnlyPolicy_AllowsAdminRole()
    {
        var result = await AuthorizeAsActiveUserAsync("AdminOnly", RoleNames.Admin);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task OrganizationStaffOrAdminPolicy_RejectsPlainUserRole()
    {
        var result = await AuthorizeAsActiveUserAsync("OrganizationStaffOrAdmin", RoleNames.User);
        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData(RoleNames.OrganizationStaff)]
    [InlineData(RoleNames.Admin)]
    public async Task OrganizationStaffOrAdminPolicy_AllowsEitherRole(string role)
    {
        var result = await AuthorizeAsActiveUserAsync("OrganizationStaffOrAdmin", role);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AdminOnlyPolicy_RejectsSuspendedAdmin()
    {
        var userId = await CreateUserAsync(UserStatus.Suspended);
        var result = await AuthorizeAsync("AdminOnly", RoleNames.Admin, userId);

        Assert.False(result.Succeeded);
    }

    private async Task<AuthorizationResult> AuthorizeAsActiveUserAsync(string policyName, string role)
    {
        var userId = await CreateUserAsync(UserStatus.Active);
        return await AuthorizeAsync(policyName, role, userId);
    }

    private async Task<AuthorizationResult> AuthorizeAsync(string policyName, string role, Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            },
            authenticationType: "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return await authorizationService.AuthorizeAsync(principal, resource: null, policyName);
    }

    private async Task<Guid> CreateUserAsync(UserStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FindoraDbContext>();

        var email = $"policy-test-{Guid.NewGuid():N}@example.test";
        var user = new User
        {
            Email = email,
            NormalizedEmail = email,
            FullName = "Policy Test User",
            PasswordHash = "not-a-real-hash",
            Status = status
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user.Id;
    }
}
