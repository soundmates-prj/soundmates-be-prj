namespace AiService.Api.Models.Responses;

public class AudioResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public Guid ScriptId { get; set; }
    public Guid VoiceId { get; set; }
    public float Speed { get; set; }
    public float Pitch { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
