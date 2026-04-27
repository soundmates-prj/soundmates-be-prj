using AccountContentService.Application.Common.Pagination;
using AccountContentService.Domain.Entities;
using System;

namespace AccountContentService.Application.Interfaces.Repositories;

public interface ISystemSettingReposiotry
{
    Task AddAsync(SystemSetting setting);
    Task UpdateAsync(SystemSetting setting);
    Task DeleteAsync(SystemSetting setting);

    Task<PaginationResult<SystemSetting>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<SystemSetting?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Upsert a system setting by key, including IsActive flag.
    /// </summary>
    Task<SystemSetting> UpsertAsync(string key, string value, string settingType, string description, bool isActive, CancellationToken cancellationToken);

    /// <summary>
    /// Delete a system setting by key. Returns true if deleted, false if not found.
    /// </summary>
    Task<bool> DeleteByKeyAsync(string key, CancellationToken cancellationToken);
}
