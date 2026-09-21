using Findora.API.DTOs.ItemCategories;

namespace Findora.API.Services;

/// <summary>Item category lookup logic (Module 4), reusable by Module 5's found-item reporting.</summary>
public interface IItemCategoryService
{
    Task<IReadOnlyList<ItemCategoryResponse>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);
}
