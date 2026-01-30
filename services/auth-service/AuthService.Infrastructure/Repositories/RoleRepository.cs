using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

/// <summary>
/// Role repository implementation
/// Direct access to DbContext through UnitOfWork (DAO layer removed)
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly IUnitOfWork _uow;
    private readonly AuthDbContext _db;

    public RoleRepository(IUnitOfWork uow)
    {
        _uow = uow;
        _db = (AuthDbContext)_uow.Context;
    }

    public async Task<UserRole?> GetByIdAsync(Guid id)
    {
        return await _db.Set<UserRole>().FindAsync(id);
    }

    public async Task<UserRole?> GetByNameAsync(string name)
    {
        return await _db.Set<UserRole>().FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<List<UserRole>> GetAllAsync()
    {
        return await _db.Set<UserRole>().ToListAsync();
    }

    public async Task AddAsync(UserRole role)
    {
        await _db.Set<UserRole>().AddAsync(role);
    }

    public async Task UpdateAsync(UserRole role)
    {
        _db.Set<UserRole>().Update(role);
    }

    public async Task DeleteAsync(UserRole role)
    {
        _db.Set<UserRole>().Remove(role);
    }
}

