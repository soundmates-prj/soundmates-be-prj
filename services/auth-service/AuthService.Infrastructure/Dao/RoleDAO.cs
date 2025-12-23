using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Dao.Interfaces;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Dao
{
    public class RoleDao : IRoleDao
    {
        private readonly IUnitOfWork _uow;
        private readonly AuthDbContext _db;

        public RoleDao(IUnitOfWork uow)
        {
            _uow = uow;
            _db = (AuthDbContext)_uow.Context;
        }

        public async Task<UserRole?> GetByIdAsync(Guid id)
            => await _db.Set<UserRole>().FindAsync(id);

        public async Task<UserRole?> GetByNameAsync(string name)
            => await _db.Set<UserRole>().FirstOrDefaultAsync(r => r.Name == name);
            
        public async Task<List<UserRole>> GetAllAsync()
            => await _db.Set<UserRole>().ToListAsync();

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
}
