using System.ComponentModel.DataAnnotations;

namespace AiService.Domain.Entities;

public class AiPrompt
{
    [Key]
    public Guid PromptId { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(50)]
    public string ContextType { get; set; } = default!;

    public string InputText { get; set; } = default!;

    [MaxLength(100)]
    public string? ModelName { get; set; }

    public decimal? Temperature { get; set; }

    public int? MaxTokens { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Script> Scripts { get; set; } = new List<Script>();
}

