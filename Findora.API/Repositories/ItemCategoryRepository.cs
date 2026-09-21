using Findora.API.Data;
using Findora.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Findora.API.Repositories;

public class ItemCategoryRepository : IItemCategoryRepository
{
    private readonly FindoraDbContext _db;

    public ItemCategoryRepository(FindoraDbContext db)
    {
        _db = db;
    }

    public Task<List<ItemCategory>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        _db.ItemCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public Task<ItemCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.ItemCategories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
}
