using AccountContentService.Application.Common.Pagination;
using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountContentService.Infrastructure.Repositories;

public class SystemSettingReposiotry : ISystemSettingReposiotry
{
    private readonly AccountContentDbContext _context;

    public SystemSettingReposiotry(AccountContentDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SystemSetting setting)
    {
        await _context.SystemSettings.AddAsync(setting);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(SystemSetting setting)
    {
        _context.SystemSettings.Remove(setting);
        await _context.SaveChangesAsync();
    }

    public async Task<PaginationResult<SystemSetting>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.SystemSettings
            .AsNoTracking()
            .AsQueryable();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginationResult<SystemSetting>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SystemSetting?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.SystemSettings
            .AsNoTracking()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        return await _context.SystemSettings
            .AsNoTracking()
            .Where(x => x.Key.ToLower() == key.ToLower())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateAsync(SystemSetting setting)
    {
        _context.SystemSettings.Update(setting);
        await _context.SaveChangesAsync();
    }

    public async Task<SystemSetting> UpsertAsync(
        string key,
        string value,
        string settingType,
        string description,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var existing = await _context.SystemSettings
            .Where(x => x.Key.ToLower() == key.ToLower())
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            existing.Value = value;
            existing.SettingType = settingType;
            existing.Description = description;
            existing.IsActive = isActive;
            existing.UpdateAt = DateTime.UtcNow;
            _context.SystemSettings.Update(existing);
        }
        else
        {
            existing = new SystemSetting
            {
                Id = Guid.NewGuid(),
                Key = key,
                Value = value,
                SettingType = settingType,
                Description = description,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            };
            await _context.SystemSettings.AddAsync(existing, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteByKeyAsync(string key, CancellationToken cancellationToken)
    {
        var existing = await _context.SystemSettings
            .Where(x => x.Key.ToLower() == key.ToLower())
            .FirstOrDefaultAsync(cancellationToken);

        if (existing == null)
        {
            return false;
        }

        _context.SystemSettings.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
