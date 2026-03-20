using System.ComponentModel.DataAnnotations;

namespace AiService.Domain.Entities;

public class TtsVoice
{
    [Key]
    public Guid VoiceId { get; set; }

    [MaxLength(50)]
    public string Provider { get; set; } = default!;

    [MaxLength(100)]
    public string VoiceCode { get; set; } = default!;

    [MaxLength(100)]
    public string DisplayName { get; set; } = default!;

    [MaxLength(50)]
    public string Region { get; set; } = default!;

    [MaxLength(20)]
    public string Gender { get; set; } = default!;

    [MaxLength(100)]
    public string Model { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<ScriptAudio> Audios { get; set; } = new List<ScriptAudio>();
}

