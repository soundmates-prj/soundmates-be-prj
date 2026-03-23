using AccountContentService.Application.Common.Pagination;
using AccountContentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Repositories
{
    public interface IThemeRepository
    {
        // Get all themes 
        Task<PaginationResult<Theme>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken);

        // Get by Id
        Task<Theme?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        // Get by Name 
        Task<PaginationResult<Theme>> GetAllByNameAsync(string name, int page, int pageSize, CancellationToken cancellationToken);

        // Get active themes
        Task<PaginationResult<Theme>> GetActiveAsync(int page, int pageSize, CancellationToken cancellationToken);

        // CRUD
        Task AddAsync(Theme theme, CancellationToken cancellationToken);
        Task UpdateAsync(Theme theme, CancellationToken cancellationToken);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken);

        // Check exists (useful for validation)
        Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
    }
}
