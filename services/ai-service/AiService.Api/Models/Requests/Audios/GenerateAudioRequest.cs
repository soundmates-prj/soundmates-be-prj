using System.ComponentModel.DataAnnotations;

namespace AiService.Api.Models.Requests.Audios;

public class GenerateAudioRequest
{
    // voiceCode: string like "bd8f365656ad409e9fd3b5f19624c58b" (matches TtsSynthesizeRequest.VoiceCode)
    [Required]
    public string VoiceCode { get; set; } = null!;

    public decimal? Speed { get; set; }

    public decimal? Pitch { get; set; }

    // Optional BGM to duck and mix (URL or pre-defined local path)
    public string? BgmUrl { get; set; }
}

