using AuthService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Domain.Interfaces
{
    public interface IRoleRepository
    {
        Task<UserRole?> GetByIdAsync(Guid id);
        Task<UserRole?> GetByNameAsync(string name);
        Task<List<UserRole>> GetAllAsync();

        Task AddAsync(UserRole role);

        Task UpdateAsync(UserRole role);

        Task DeleteAsync(UserRole role);
    }
}
