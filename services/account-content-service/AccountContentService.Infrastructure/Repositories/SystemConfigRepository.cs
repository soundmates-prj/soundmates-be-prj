using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Interfaces;
using AccountContentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccountContentService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for SystemConfig entity
/// </summary>
public class SystemConfigRepository : ISystemConfigRepository
{
    private readonly AccountContentDbContext _context;

    public SystemConfigRepository(AccountContentDbContext context)
    {
        _context = context;
    }

    public async Task<SystemConfig?> GetByKeyAsync(string configKey, CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.ConfigKey == configKey, cancellationToken);
    }

    public async Task<List<SystemConfig>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigs
            .OrderBy(c => c.Category)
            .ThenBy(c => c.ConfigKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SystemConfig>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigs
            .Where(c => c.Category == category)
            .OrderBy(c => c.ConfigKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SystemConfig>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigs
            .Where(c => c.IsActive)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.ConfigKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<SystemConfig> AddAsync(SystemConfig config, CancellationToken cancellationToken = default)
    {
        await _context.SystemConfigs.AddAsync(config, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return config;
    }

    public async Task UpdateAsync(SystemConfig config, CancellationToken cancellationToken = default)
    {
        _context.SystemConfigs.Update(config);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(SystemConfig config, CancellationToken cancellationToken = default)
    {
        _context.SystemConfigs.Remove(config);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string configKey, CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigs
            .AnyAsync(c => c.ConfigKey == configKey, cancellationToken);
    }
}
