using AccountContentService.Application.Interfaces;
using AccountContentService.Application.Interfaces.Services;
using AccountContentService.Domain.Entities;
using AccountContentService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AccountContentService.Application.Services;

/// <summary>
/// Service implementation for managing system configurations
/// </summary>
public class SystemConfigService : ISystemConfigService
{
    private readonly ISystemConfigRepository _repository;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<SystemConfigService> _logger;

    public SystemConfigService(
        ISystemConfigRepository repository,
        IEncryptionService encryptionService,
        ILogger<SystemConfigService> logger)
    {
        _repository = repository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<string?> GetConfigValueAsync(string configKey, CancellationToken cancellationToken = default)
    {
        var config = await _repository.GetByKeyAsync(configKey, cancellationToken);
        
        if (config == null || !config.IsActive)
            return null;

        try
        {
            return config.IsEncrypted 
                ? _encryptionService.Decrypt(config.ConfigValue) 
                : config.ConfigValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt config value for key: {ConfigKey}", configKey);
            return null;
        }
    }

    public async Task<SystemConfig?> GetConfigAsync(string configKey, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByKeyAsync(configKey, cancellationToken);
    }

    public async Task<List<SystemConfig>> GetAllConfigsAsync(bool includeSensitive = false, CancellationToken cancellationToken = default)
    {
        var configs = await _repository.GetAllAsync(cancellationToken);
        
        if (!includeSensitive)
        {
            return MaskSensitiveValues(configs);
        }

        return configs;
    }

    public async Task<List<SystemConfig>> GetConfigsByCategoryAsync(string category, bool includeSensitive = false, CancellationToken cancellationToken = default)
    {
        var configs = await _repository.GetByCategoryAsync(category, cancellationToken);
        
        if (!includeSensitive)
        {
            return MaskSensitiveValues(configs);
        }

        return configs;
    }

    public async Task<SystemConfig> SetConfigAsync(
        string configKey,
        string configValue,
        string category,
        string? description = null,
        bool isEncrypted = false,
        bool isSensitive = false,
        Guid? updatedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var existingConfig = await _repository.GetByKeyAsync(configKey, cancellationToken);

        // Encrypt value if needed
        var valueToStore = isEncrypted 
            ? _encryptionService.Encrypt(configValue) 
            : configValue;

        if (existingConfig != null)
        {
            // Update existing
            existingConfig.UpdateValue(valueToStore, updatedByUserId);
            await _repository.UpdateAsync(existingConfig, cancellationToken);
            
            _logger.LogInformation(
                "Updated config: {ConfigKey} in category {Category} by user {UserId}",
                configKey, category, updatedByUserId);
            
            return existingConfig;
        }
        else
        {
            // Create new
            var newConfig = SystemConfig.Create(
                configKey,
                valueToStore,
                category,
                description,
                isEncrypted,
                isSensitive);

            await _repository.AddAsync(newConfig, cancellationToken);
            
            _logger.LogInformation(
                "Created config: {ConfigKey} in category {Category}",
                configKey, category);
            
            return newConfig;
        }
    }

    public async Task<SystemConfig> UpdateConfigValueAsync(
        string configKey,
        string newValue,
        Guid? updatedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var config = await _repository.GetByKeyAsync(configKey, cancellationToken);
        
        if (config == null)
            throw new KeyNotFoundException($"Configuration with key '{configKey}' not found");

        // Encrypt value if config is marked as encrypted
        var valueToStore = config.IsEncrypted 
            ? _encryptionService.Encrypt(newValue) 
            : newValue;

        config.UpdateValue(valueToStore, updatedByUserId);
        await _repository.UpdateAsync(config, cancellationToken);

        _logger.LogInformation(
            "Updated config value: {ConfigKey} by user {UserId}",
            configKey, updatedByUserId);

        return config;
    }

    public async Task<bool> DeleteConfigAsync(string configKey, CancellationToken cancellationToken = default)
    {
        var config = await _repository.GetByKeyAsync(configKey, cancellationToken);
        
        if (config == null)
            return false;

        await _repository.DeleteAsync(config, cancellationToken);
        
        _logger.LogInformation("Deleted config: {ConfigKey}", configKey);
        
        return true;
    }

    public async Task<bool> ActivateConfigAsync(string configKey, Guid? updatedByUserId = null, CancellationToken cancellationToken = default)
    {
        var config = await _repository.GetByKeyAsync(configKey, cancellationToken);
        
        if (config == null)
            return false;

        config.Activate(updatedByUserId);
        await _repository.UpdateAsync(config, cancellationToken);
        
        _logger.LogInformation(
            "Activated config: {ConfigKey} by user {UserId}",
            configKey, updatedByUserId);
        
        return true;
    }

    public async Task<bool> DeactivateConfigAsync(string configKey, Guid? updatedByUserId = null, CancellationToken cancellationToken = default)
    {
        var config = await _repository.GetByKeyAsync(configKey, cancellationToken);
        
        if (config == null)
            return false;

        config.Deactivate(updatedByUserId);
        await _repository.UpdateAsync(config, cancellationToken);
        
        _logger.LogInformation(
            "Deactivated config: {ConfigKey} by user {UserId}",
            configKey, updatedByUserId);
        
        return true;
    }

    /// <summary>
    /// Mask sensitive configuration values
    /// </summary>
    private List<SystemConfig> MaskSensitiveValues(List<SystemConfig> configs)
    {
        // Create a copy to avoid modifying the original entities
        return configs.Select(c =>
        {
            if (c.IsSensitive)
            {
                // Return a masked version
                return SystemConfig.Create(
                    c.ConfigKey,
                    "***MASKED***",
                    c.Category,
                    c.Description,
                    c.IsEncrypted,
                    c.IsSensitive,
                    c.IsActive);
            }
            return c;
        }).ToList();
    }
}
