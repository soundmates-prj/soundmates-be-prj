using AuthService.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace AuthService.Domain.Interfaces
{
    public interface IProfileRepository
    {
        Task<Profile?> GetByUserIdAsync(Guid userId);
        Task<Profile> CreateAsync(Profile profile);
        Task UpdateAsync(Profile profile);
    }
}

