using Findora.API.Models;

namespace Findora.API.Repositories;

/// <summary>Data-access abstraction for <see cref="ItemCategory"/> lookups (Module 4).</summary>
public interface IItemCategoryRepository
{
    /// <summary>All active, non-deleted categories, ordered by name.</summary>
    Task<List<ItemCategory>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Fetches a single category by id (excluding soft-deleted), regardless of <see cref="ItemCategory.IsActive"/> — used for FK validation.</summary>
    Task<ItemCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
