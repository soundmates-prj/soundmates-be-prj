using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

/// <summary>
/// User repository implementation
/// Direct access to DbContext through UnitOfWork (DAO layer removed)
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IUnitOfWork _uow;
    private readonly AuthDbContext _db;

    public UserRepository(IUnitOfWork uow)
    {
        _uow = uow;
        _db = (AuthDbContext)_uow.Context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _db.Set<User>()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _db.Set<User>()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _db.Set<User>()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _db.Set<User>()
            .Include(u => u.Role)
            .ToListAsync();
    }

    public async Task<(List<User> Items, int TotalCount)> GetPagedAsync(int page, int size)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 20;

        var query = _db.Set<User>()
            .Include(u => u.Role)
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt ?? DateTime.UnixEpoch);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(User user)
    {
        _db.Set<User>().Add(user);
        await _uow.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _db.Set<User>().Update(user);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            _db.Set<User>().Remove(user);
            await _uow.SaveChangesAsync();
        }
    }
}

