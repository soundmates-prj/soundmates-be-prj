namespace AccountContentService.Api.Contracts.Responses;

/// <summary>
/// Response for system configuration
/// </summary>
public class SystemConfigResponse
{
    public Guid Id { get; set; }
    public string ConfigKey { get; set; } = null!;
    public string ConfigValue { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsActive { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
