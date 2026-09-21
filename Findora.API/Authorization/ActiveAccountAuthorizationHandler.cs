using System.Security.Claims;
using Findora.API.Data;
using Findora.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Findora.API.Authorization;

/// <summary>
/// Looks up the current status of the authenticated principal's account on
/// every request that requires <see cref="ActiveAccountRequirement"/>, and
/// fails the requirement (→ 403, not 401 — the token itself is valid) for
/// anything other than <see cref="UserStatus.Active"/>. One extra indexed
/// lookup per authenticated request is a deliberate, simple trade-off for
/// immediate enforcement — appropriate at this project's scale.
/// </summary>
public class ActiveAccountAuthorizationHandler : AuthorizationHandler<ActiveAccountRequirement>
{
    private readonly FindoraDbContext _db;

    public ActiveAccountAuthorizationHandler(FindoraDbContext db)
    {
        _db = db;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveAccountRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue is null || !Guid.TryParse(userIdValue, out var userId))
        {
            // No parseable identity — leave the requirement unsatisfied;
            // DenyAnonymousAuthenticationRequirement (also on the default
            // policy) is what produces the actual 401 for this case.
            return;
        }

        var status = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => (UserStatus?)u.Status)
            .FirstOrDefaultAsync();

        if (status == UserStatus.Active)
        {
            context.Succeed(requirement);
        }
    }
}
