using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Dao.Interfaces;
using System;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Repositories
{
    public class ProfileRepository : IProfileRepository
    {
        private readonly IProfileDAO _profileDAO;

        public ProfileRepository(IProfileDAO profileDAO)
        {
            _profileDAO = profileDAO;
        }

        public async Task<Profile?> GetByUserIdAsync(Guid userId)
        {
            return await _profileDAO.GetByUserIdAsync(userId);
        }

        public async Task<Profile> CreateAsync(Profile profile)
        {
            return await _profileDAO.CreateAsync(profile);
        }

        public async Task UpdateAsync(Profile profile)
        {
            await _profileDAO.UpdateAsync(profile);
        }
    }
}

