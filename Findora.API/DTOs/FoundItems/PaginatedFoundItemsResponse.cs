namespace Findora.API.DTOs.FoundItems;

/// <summary>Paginated shape for <c>GET /api/v1/found-items/my</c>.</summary>
public class PaginatedFoundItemsResponse
{
    public IReadOnlyList<FoundItemSummaryResponse> Items { get; set; } = Array.Empty<FoundItemSummaryResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
