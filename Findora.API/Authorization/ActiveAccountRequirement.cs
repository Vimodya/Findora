using Microsoft.AspNetCore.Authorization;

namespace Findora.API.Authorization;

/// <summary>
/// Requirement (Module 3): the authenticated principal's account must
/// currently be <see cref="Models.UserStatus.Active"/>. Added to the
/// default authorization policy in <c>Program.cs</c>, so it applies to
/// every <c>[Authorize]</c>/<c>[Authorize(Roles = ...)]</c>-protected
/// endpoint automatically — a suspended/deactivated user's still-valid,
/// unexpired access token stops working the moment their status changes,
/// not just the next time they try to log in.
/// </summary>
public class ActiveAccountRequirement : IAuthorizationRequirement
{
}
