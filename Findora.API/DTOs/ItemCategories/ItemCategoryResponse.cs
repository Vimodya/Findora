namespace Findora.API.DTOs.ItemCategories;

/// <summary>Shape returned by <c>GET /api/v1/item-categories</c> — usable by both Lost (Module 4) and Found (Module 5) reporting frontends.</summary>
public class ItemCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
