using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthQueryService.Domain.Entities.ReadModels;
using AuthQueryService.Domain.Interfaces;
using AuthQueryService.Infrastructure.DAO.Interfaces;

namespace AuthQueryService.Infrastructure.Repositories
{
    public sealed class UserReadRepository : IUserReadRepository
    {
        private readonly IUserReadDAO _dao;

        public UserReadRepository(IUserReadDAO dao)
        {
            _dao = dao;
        }

        public Task<UserReadModel?> GetByIdAsync(Guid id)
            => _dao.GetByIdAsync(id);

        public Task<UserReadModel?> GetByUsernameAsync(string username)
            => _dao.GetByUsernameAsync(username);

        public Task<List<UserReadModel>> GetAllAsync()
            => _dao.GetAllAsync();

        public Task<(List<UserReadModel> Items, int TotalCount)> GetPagedAsync(int page, int size)
            => _dao.GetPagedAsync(page, size);

        public Task<(List<UserReadModel> Items, int TotalCount)> SearchAsync(string? searchTerm, int page, int size)
            => _dao.SearchAsync(searchTerm, page, size);

        public Task UpsertAsync(UserReadModel model)
            => _dao.UpsertAsync(model);

        public Task DeleteAsync(Guid id)
            => _dao.DeleteAsync(id);
    }
}

