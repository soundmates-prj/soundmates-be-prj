namespace AccountContentService.Domain.Entities;

public class ServiceConfig
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
