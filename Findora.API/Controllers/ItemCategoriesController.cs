using Findora.API.DTOs.ItemCategories;
using Findora.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Findora.API.Controllers;

/// <summary>
/// Category lookup shared by Lost (Module 4) and Found (Module 5)
/// reporting frontends. Anonymous — there's no reason to require a login
/// just to see the category list, and both report-creation forms need it
/// before the user is necessarily mid-flow.
/// </summary>
[ApiController]
[Route("api/v1/item-categories")]
public class ItemCategoriesController : ControllerBase
{
    private readonly IItemCategoryService _itemCategoryService;

    public ItemCategoriesController(IItemCategoryService itemCategoryService)
    {
        _itemCategoryService = itemCategoryService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ItemCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var categories = await _itemCategoryService.GetActiveCategoriesAsync(cancellationToken);
        return Ok(categories);
    }
}
