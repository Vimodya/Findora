using System.Security.Claims;
using Findora.API.DTOs.Auth;
using Findora.API.DTOs.LostItems;
using Findora.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Findora.API.Controllers;

/// <summary>
/// Module 4 — Lost Item Reporting. Thin, like <c>UsersController</c>: binds/
/// validates the request, delegates to <see cref="ILostItemService"/>, maps
/// the result to an HTTP response.
///
/// Visibility decision: for this MVP pass, <see cref="GetById"/> is
/// owner-only (a user can only retrieve their own report by id). A future
/// module (Search — Module 7, or Matching — Module 8/9) will need broader
/// read access to lost reports to power search/matching; when that lands,
/// it should go through a dedicated, deliberately-scoped read surface
/// (e.g. a search endpoint returning a reduced/anonymized shape) rather
/// than widening this endpoint, since <see cref="LostItemResponse"/>
/// currently includes the reporter's raw <c>UserId</c>.
/// </summary>
[ApiController]
[Route("api/v1/lost-items")]
[Authorize]
public class LostItemsController : ControllerBase
{
    private readonly ILostItemService _lostItemService;

    public LostItemsController(ILostItemService lostItemService)
    {
        _lostItemService = lostItemService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(LostItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateLostItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _lostItemService.CreateAsync(GetCurrentUserId(), request, cancellationToken);

        if (!result.Succeeded)
        {
            return ToErrorResult(result.ErrorCode, result.ErrorMessage!);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(PaginatedLostItemsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMy([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _lostItemService.GetOwnReportsAsync(GetCurrentUserId(), page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LostItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _lostItemService.GetByIdAsync(GetCurrentUserId(), id, cancellationToken);

        if (!result.Succeeded)
        {
            return ToErrorResult(result.ErrorCode, result.ErrorMessage!);
        }

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LostItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLostItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _lostItemService.UpdateAsync(GetCurrentUserId(), id, request, cancellationToken);

        if (!result.Succeeded)
        {
            return ToErrorResult(result.ErrorCode, result.ErrorMessage!);
        }

        return Ok(result.Value);
    }

    /// <summary>Cancels (soft-deletes) the report. Owner only. Never physically removes the database row.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(LostItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _lostItemService.CancelAsync(GetCurrentUserId(), id, cancellationToken);

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
