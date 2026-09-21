using Findora.API.Models;

namespace Findora.API.Repositories;

/// <summary>Data-access abstraction for <see cref="FoundItemReport"/> (Module 5), mirroring <see cref="ILostItemRepository"/>.</summary>
public interface IFoundItemRepository
{
    Task<FoundItemReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Fetches a report by id with its category eagerly loaded, for mapping to a response DTO.</summary>
    Task<FoundItemReport?> GetByIdWithCategoryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Page of the given user's own reports (newest first), with category eagerly loaded.</summary>
    Task<(List<FoundItemReport> Items, int TotalCount)> GetByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(FoundItemReport report);
}
