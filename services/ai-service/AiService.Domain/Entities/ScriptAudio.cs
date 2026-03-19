using System.ComponentModel.DataAnnotations;

namespace AiService.Domain.Entities;

public class ScriptAudio
{
    [Key]
    public Guid AudioId { get; set; }

    public string AudioUrl { get; set; } = default!;

    public string AudioPath { get; set; } = default!;

    public decimal? Speed { get; set; }

    public decimal? Pitch { get; set; }

    public int? Duration { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = default!;

    public Guid ScriptId { get; set; }
    public Script Script { get; set; } = default!;

    public Guid VoiceId { get; set; }
    public TtsVoice Voice { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

