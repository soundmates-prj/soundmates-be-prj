using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthQueryService.Domain.Entities.ReadModels;

namespace AuthQueryService.Domain.Interfaces
{
    public interface IUserReadRepository
    {
        Task<UserReadModel?> GetByIdAsync(Guid id);
        Task<UserReadModel?> GetByUsernameAsync(string username);
        Task<List<UserReadModel>> GetAllAsync();
        Task<(List<UserReadModel> Items, int TotalCount)> GetByRoleAsync(string roleName, int page, int size);
        Task<(List<UserReadModel> Items, int TotalCount)> GetPagedAsync(int page, int size);
        Task<(List<UserReadModel> Items, int TotalCount)> SearchAsync(string? searchTerm, int page, int size);
        Task UpsertAsync(UserReadModel model);
        Task DeleteAsync(Guid id);
    }
}