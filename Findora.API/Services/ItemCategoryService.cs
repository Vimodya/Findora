using Findora.API.DTOs.ItemCategories;
using Findora.API.Mappings;
using Findora.API.Repositories;

namespace Findora.API.Services;

public class ItemCategoryService : IItemCategoryService
{
    private readonly IItemCategoryRepository _categoryRepository;

    public ItemCategoryService(IItemCategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<ItemCategoryResponse>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetActiveAsync(cancellationToken);
        return categories.Select(c => c.ToResponse()).ToArray();
    }
}
