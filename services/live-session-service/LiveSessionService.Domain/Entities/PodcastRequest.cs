using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Entities;

public class PodcastRequest
{
    public Guid Id { get; set; }
    public Guid LiveSessionId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string ScriptText { get; set; } = null!;
    public string AudioUrl { get; set; } = null!;
    public int DurationSeconds { get; set; }
    public string VoiceCode { get; set; } = null!;
    public string? VoiceDisplayName { get; set; }
    public string? AzuraCastMediaId { get; set; }
    public PodcastRequestStatus Status { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectReason { get; set; }
    public DateTime RequestedAt { get; set; }

    public virtual LiveSession LiveSession { get; set; } = null!;
}