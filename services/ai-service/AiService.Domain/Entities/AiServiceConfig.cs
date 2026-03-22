using System.ComponentModel.DataAnnotations;

namespace AiService.Domain.Entities;

public class AiServiceConfig
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(50)]
    public string Provider { get; set; } = default!;

    public string ApiKey { get; set; } = default!;

    public string? PromptTemplate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
