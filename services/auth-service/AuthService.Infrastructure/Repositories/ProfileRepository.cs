using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

/// <summary>
/// Profile repository implementation
/// Direct access to DbContext through UnitOfWork (DAO layer removed)
/// </summary>
public class ProfileRepository : IProfileRepository
{
    private readonly IUnitOfWork _uow;
    private readonly AuthDbContext _db;

    public ProfileRepository(IUnitOfWork uow)
    {
        _uow = uow;
        _db = (AuthDbContext)_uow.Context;
    }

    public async Task<Profile?> GetByUserIdAsync(Guid userId)
    {
        return await _db.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<Profile> CreateAsync(Profile profile)
    {
        profile.Id = Guid.NewGuid();
        profile.CreatedAt = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;

        _db.Profiles.Add(profile);
        await _uow.SaveChangesAsync();

        return profile;
    }

    public async Task UpdateAsync(Profile profile)
    {
        profile.UpdatedAt = DateTime.UtcNow;
        _db.Profiles.Update(profile);
        await _uow.SaveChangesAsync();
    }
}

