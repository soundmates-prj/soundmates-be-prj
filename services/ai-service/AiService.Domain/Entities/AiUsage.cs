using System.ComponentModel.DataAnnotations;

namespace AiService.Domain.Entities;

public class AiUsage
{
    [Key]
    public Guid UsageId { get; set; }

    public Guid UserId { get; set; }

    [MaxLength(50)]
    public string Provider { get; set; } = default!;

    public int? TokensUsed { get; set; }

    public decimal? Cost { get; set; }

    public Guid? ScriptId { get; set; }
    public Script? Script { get; set; }

    public DateTime CreatedAt { get; set; }
}

