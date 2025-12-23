using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Dao.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IUserDao _dao;

        public UserRepository(IUserDao dao) => _dao = dao;

        public Task<User?> GetByIdAsync(Guid id) => _dao.GetByIdAsync(id);
        public Task<User?> GetByEmailAsync(string email) => _dao.GetByEmailAsync(email);
        public Task<User?> GetByUsernameAsync(string username) => _dao.GetByUsernameAsync(username);
        public Task<List<User>> GetAllAsync() => _dao.GetAllAsync();
        public Task<(List<User> Items, int TotalCount)> GetPagedAsync(int page, int size) => _dao.GetPagedAsync(page, size);
        public Task AddAsync(User user) => _dao.AddAsync(user);
        public Task UpdateAsync(User user) => _dao.UpdateAsync(user);
        public Task DeleteAsync(Guid id) => _dao.DeleteAsync(id);
    }
}
