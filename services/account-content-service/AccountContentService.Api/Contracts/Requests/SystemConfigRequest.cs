using System.ComponentModel.DataAnnotations;

namespace AccountContentService.Api.Contracts.Requests;

/// <summary>
/// Request to create or update system configuration
/// </summary>
public class SystemConfigRequest
{
    /// <summary>
    /// Configuration key (unique identifier)
    /// </summary>
    [Required(ErrorMessage = "Config key is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Config key must be between 3 and 100 characters")]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Config key can only contain letters, numbers, dots, hyphens and underscores")]
    public string ConfigKey { get; set; } = null!;

    /// <summary>
    /// Configuration value
    /// </summary>
    [Required(ErrorMessage = "Config value is required")]
    public string ConfigValue { get; set; } = null!;

    /// <summary>
    /// Configuration category (e.g., "AI", "Integration", "Security")
    /// </summary>
    [Required(ErrorMessage = "Category is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Category must be between 2 and 50 characters")]
    public string Category { get; set; } = null!;

    /// <summary>
    /// Description of what this config does
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this value should be encrypted
    /// </summary>
    public bool IsEncrypted { get; set; } = false;

    /// <summary>
    /// Whether this config is sensitive (should not be exposed in logs)
    /// </summary>
    public bool IsSensitive { get; set; } = false;
}

/// <summary>
/// Request to update configuration value only
/// </summary>
public class UpdateConfigValueRequest
{
    /// <summary>
    /// New configuration value
    /// </summary>
    [Required(ErrorMessage = "Config value is required")]
    public string ConfigValue { get; set; } = null!;
}
