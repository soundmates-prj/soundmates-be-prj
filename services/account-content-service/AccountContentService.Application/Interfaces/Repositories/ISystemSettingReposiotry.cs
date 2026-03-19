using AccountContentService.Application.Common.Pagination;
using AccountContentService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AccountContentService.Application.Interfaces.Repositories
{
    public interface ISystemSettingReposiotry
    {
        /// <summary>
        /// CRUD operations for system setting
        ///</summary>
        Task AddAsync(SystemSetting setting);
        Task UpdateAsync(SystemSetting setting);
        Task DeleteAsync(SystemSetting setting);


        Task<PaginationResult<SystemSetting>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken);
        Task<SystemSetting> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<SystemSetting> GetByKeyAsync(string key, CancellationToken cancellationToken);
    }
}
