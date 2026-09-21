using System.Security.Claims;
using Findora.API.DTOs.Auth;
using Findora.API.DTOs.FoundItems;
using Findora.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Findora.API.Controllers;

/// <summary>
/// Module 5 — Found Item Reporting. Mirrors <c>LostItemsController</c>
/// exactly: thin, binds/validates the request, delegates to
/// <see cref="IFoundItemService"/>, maps the result to an HTTP response.
///
/// Visibility decision: same as Module 4 — <see cref="GetById"/> is
/// owner-only for this MVP pass. A future module (Search — Module 7, or
/// Matching — Module 8/9) will need broader read access to found reports;
/// when that lands, it should go through a dedicated, deliberately-scoped
/// read surface rather than widening this endpoint, since
/// <see cref="FoundItemResponse"/> currently includes the finder's raw
/// <c>UserId</c>.
///
/// Category lookup is shared with Module 4 — see <c>ItemCategoriesController</c>
/// (<c>GET /api/v1/item-categories</c>); there is no separate
/// found-item-categories endpoint.
/// </summary>
[ApiController]
[Route("api/v1/found-items")]
[Authorize]
public class FoundItemsController : ControllerBase
{
    private readonly IFoundItemService _foundItemService;

    public FoundItemsController(IFoundItemService foundItemService)
    {
        _foundItemService = foundItemService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(FoundItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateFoundItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _foundItemService.CreateAsync(GetCurrentUserId(), request, cancellationToken);

        if (!result.Succeeded)
        {
            return ToErrorResult(result.ErrorCode, result.ErrorMessage!);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(PaginatedFoundItemsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMy([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _foundItemService.GetOwnReportsAsync(GetCurrentUserId(), page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FoundItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _foundItemService.GetByIdAsync(GetCurrentUserId(), id, cancellationToken);

        if (!result.Succeeded)
        {
            return ToErrorResult(result.ErrorCode, result.ErrorMessage!);
        }

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FoundItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFoundItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _foundItemService.UpdateAsync(GetCurrentUserId(), id, request, cancellationToken);

        if (!result.Succeeded)
        {
            return ToErrorResult(result.ErrorCode, result.ErrorMessage!);
        }

        return Ok(result.Value);
    }

    /// <summary>Cancels (soft-deletes) the report. Finder only. Never physically removes the database row.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(FoundItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _foundItemService.CancelAsync(GetCurrentUserId(), id, cancellationToken);

        if (!result.Succeeded)
        {
            return ToErrorResult(result.ErrorCode, result.ErrorMessage!);
        }

        return Ok(result.Value);
    }

    private IActionResult ToErrorResult(ServiceErrorCode errorCode, string message) => errorCode switch
    {
        ServiceErrorCode.NotFound => NotFound(new MessageResponse(message)),
        ServiceErrorCode.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new MessageResponse(message)),
        ServiceErrorCode.Validation => BadRequest(new MessageResponse(message)),
        _ => BadRequest(new MessageResponse(message))
    };

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
