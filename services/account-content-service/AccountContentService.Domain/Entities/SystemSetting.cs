namespace AccountContentService.Domain.Entities;

public class SystemSetting
{
    public Guid Id { get; set; }

    /// <summary>
    /// Unique key, e.g. "gemini:apikey", "azuracast:apikey", "vnpay:secret"
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Stored value (JSON payload, API key, secret, etc.)
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Category for grouping, e.g. "gemini", "azuracast", "vnpay", "system"
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Setting type, e.g. "encrypted", "sensitive", "public"
    /// </summary>
    public string SettingType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this config is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the value is encrypted at rest.
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Whether the value is sensitive (never exposed in logs/API responses).
    /// </summary>
    public bool IsSensitive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdateAt { get; set; }

    /// <summary>
    /// Provider name — used for service configs (e.g. "gemini", "azuracast", "vnpay")
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// ID of the user who last updated this setting.
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }
}
