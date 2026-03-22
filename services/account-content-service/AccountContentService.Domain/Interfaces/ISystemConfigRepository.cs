using AccountContentService.Domain.Entities;

namespace AccountContentService.Domain.Interfaces;

/// <summary>
/// Repository interface for SystemConfig entity
/// </summary>
public interface ISystemConfigRepository
{
    /// <summary>
    /// Get configuration by key
    /// </summary>
    Task<SystemConfig?> GetByKeyAsync(string configKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all configurations
    /// </summary>
    Task<List<SystemConfig>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get configurations by category
    /// </summary>
    Task<List<SystemConfig>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active configurations only
    /// </summary>
    Task<List<SystemConfig>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new configuration
    /// </summary>
    Task<SystemConfig> AddAsync(SystemConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing configuration
    /// </summary>
    Task UpdateAsync(SystemConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete configuration
    /// </summary>
    Task DeleteAsync(SystemConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if configuration key exists
    /// </summary>
    Task<bool> ExistsAsync(string configKey, CancellationToken cancellationToken = default);
}
