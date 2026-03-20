using System.ComponentModel.DataAnnotations;

namespace AiService.Api.Models.Requests.Audios;

public class GenerateAudioRequest
{
    [Required]
    public Guid VoiceId { get; set; }

    public decimal? Speed { get; set; }

    public decimal? Pitch { get; set; }
}

