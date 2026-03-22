using AccountContentService.Domain.Common;

namespace AccountContentService.Domain.Entities;

/// <summary>
/// System configuration entity for storing dynamic settings
/// </summary>
public class SystemConfig : BaseEntity
{
    /// <summary>
    /// Configuration key (unique identifier)
    /// </summary>
    public string ConfigKey { get; private set; } = null!;

    /// <summary>
    /// Configuration value (encrypted if sensitive)
    /// </summary>
    public string ConfigValue { get; private set; } = null!;

    /// <summary>
    /// Configuration category (e.g., "AI", "Integration", "Security")
    /// </summary>
    public string Category { get; private set; } = null!;

    /// <summary>
    /// Description of what this config does
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Whether this value is encrypted
    /// </summary>
    public bool IsEncrypted { get; private set; }

    /// <summary>
    /// Whether this config is sensitive (should not be exposed in logs)
    /// </summary>
    public bool IsSensitive { get; private set; }

    /// <summary>
    /// Whether this config is active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Last updated by user ID
    /// </summary>
    public Guid? UpdatedByUserId { get; private set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    // Private constructor for EF Core
    private SystemConfig() { }

    /// <summary>
    /// Create a new system configuration
    /// </summary>
    public static SystemConfig Create(
        string configKey,
        string configValue,
        string category,
        string? description = null,
        bool isEncrypted = false,
        bool isSensitive = false,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(configKey))
            throw new ArgumentException("Config key cannot be empty", nameof(configKey));

        if (string.IsNullOrWhiteSpace(configValue))
            throw new ArgumentException("Config value cannot be empty", nameof(configValue));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));

        return new SystemConfig
        {
            Id = Guid.NewGuid(),
            ConfigKey = configKey.Trim(),
            ConfigValue = configValue,
            Category = category.Trim(),
            Description = description?.Trim(),
            IsEncrypted = isEncrypted,
            IsSensitive = isSensitive,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Update configuration value
    /// </summary>
    public void UpdateValue(string newValue, Guid? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(newValue))
            throw new ArgumentException("Config value cannot be empty", nameof(newValue));

        ConfigValue = newValue;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update configuration details
    /// </summary>
    public void Update(
        string? configValue = null,
        string? description = null,
        bool? isActive = null,
        Guid? updatedByUserId = null)
    {
        if (configValue != null && !string.IsNullOrWhiteSpace(configValue))
            ConfigValue = configValue;

        if (description != null)
            Description = description.Trim();

        if (isActive.HasValue)
            IsActive = isActive.Value;

        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activate configuration
    /// </summary>
    public void Activate(Guid? updatedByUserId = null)
    {
        IsActive = true;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivate configuration
    /// </summary>
    public void Deactivate(Guid? updatedByUserId = null)
    {
        IsActive = false;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }
}
