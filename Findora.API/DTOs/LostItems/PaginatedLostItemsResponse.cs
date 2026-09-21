namespace Findora.API.DTOs.LostItems;

/// <summary>Paginated shape for <c>GET /api/v1/lost-items/my</c>.</summary>
public class PaginatedLostItemsResponse
{
    public IReadOnlyList<LostItemSummaryResponse> Items { get; set; } = Array.Empty<LostItemSummaryResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
