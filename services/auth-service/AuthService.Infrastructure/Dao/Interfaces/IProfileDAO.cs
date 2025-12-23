using AuthService.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Dao.Interfaces
{
    public interface IProfileDAO
    {
        Task<Profile?> GetByUserIdAsync(Guid userId);
        Task<Profile> CreateAsync(Profile profile);
        Task UpdateAsync(Profile profile);
    }
}

