using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Dao.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly IRoleDao _dao;
        public RoleRepository(IRoleDao dao) => _dao = dao;

        public Task<UserRole?> GetByIdAsync(Guid id) => _dao.GetByIdAsync(id);
        public Task<List<UserRole>> GetAllAsync() => _dao.GetAllAsync();
        public Task<UserRole?> GetByNameAsync(string name) => _dao.GetByNameAsync(name);
        public Task AddAsync(UserRole role) => _dao.AddAsync(role);
        public Task UpdateAsync(UserRole role) => _dao.UpdateAsync(role);
        public Task DeleteAsync(UserRole role) => _dao.DeleteAsync(role);
    }
}
