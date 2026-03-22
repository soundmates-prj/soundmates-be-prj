using AccountContentService.Domain.Entities;

namespace AccountContentService.Application.Interfaces;

/// <summary>
/// Service interface for managing system configurations
/// </summary>
public interface ISystemConfigService
{
    /// <summary>
    /// Get configuration value by key (decrypted if encrypted)
    /// </summary>
    Task<string?> GetConfigValueAsync(string configKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get configuration by key
    /// </summary>
    Task<SystemConfig?> GetConfigAsync(string configKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all configurations (with sensitive values masked)
    /// </summary>
    Task<List<SystemConfig>> GetAllConfigsAsync(bool includeSensitive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get configurations by category
    /// </summary>
    Task<List<SystemConfig>> GetConfigsByCategoryAsync(string category, bool includeSensitive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set or update configuration
    /// </summary>
    Task<SystemConfig> SetConfigAsync(
        string configKey,
        string configValue,
        string category,
        string? description = null,
        bool isEncrypted = false,
        bool isSensitive = false,
        Guid? updatedByUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update configuration value
    /// </summary>
    Task<SystemConfig> UpdateConfigValueAsync(
        string configKey,
        string newValue,
        Guid? updatedByUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete configuration
    /// </summary>
    Task<bool> DeleteConfigAsync(string configKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activate configuration
    /// </summary>
    Task<bool> ActivateConfigAsync(string configKey, Guid? updatedByUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate configuration
    /// </summary>
    Task<bool> DeactivateConfigAsync(string configKey, Guid? updatedByUserId = null, CancellationToken cancellationToken = default);
}
