using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Dao.Interfaces;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Dao
{
    public class ProfileDAO : IProfileDAO
    {
        private readonly IUnitOfWork _uow;
        private readonly AuthDbContext _db;

        public ProfileDAO(IUnitOfWork uow)
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
}

