using Findora.API.Models;

namespace Findora.API.Repositories;

/// <summary>Data-access abstraction for <see cref="LostItemReport"/> (Module 4).</summary>
public interface ILostItemRepository
{
    Task<LostItemReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Fetches a report by id with its category eagerly loaded, for mapping to a response DTO.</summary>
    Task<LostItemReport?> GetByIdWithCategoryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Page of the given user's own reports (newest first), with category eagerly loaded.</summary>
    Task<(List<LostItemReport> Items, int TotalCount)> GetByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(LostItemReport report);
}
