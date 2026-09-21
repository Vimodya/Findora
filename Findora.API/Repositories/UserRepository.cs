using Findora.API.Data;
using Findora.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Findora.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly FindoraDbContext _db;

    public UserRepository(FindoraDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

    public Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
}
