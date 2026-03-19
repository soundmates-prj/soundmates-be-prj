using System.ComponentModel.DataAnnotations;

namespace AiService.Api.Models.Requests.Voices;

public enum VoiceType
{
    BuiltIn = 1,
    User = 2
}

public class CreateVoiceRequest
{
    [Required]
    public VoiceType VoiceType { get; set; }

    [MaxLength(50)]
    public string Provider { get; set; } = "vienetts";

    [MaxLength(100)]
    public string? VoiceCode { get; set; }

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = default!;

    [MaxLength(50)]
    public string Region { get; set; } = "VN";

    [MaxLength(20)]
    public string Gender { get; set; } = "unknown";

    [MaxLength(100)]
    public string Model { get; set; } = "pnnbao-ump/VieNeu-TTS";

    public bool IsActive { get; set; } = true;
}
