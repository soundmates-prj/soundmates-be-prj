namespace LiveSessionService.Application.Features.Results.PodcastRequests;

public sealed class PodcastRequestResult
{
    public Guid Id { get; init; }
    public Guid LiveSessionId { get; init; }
    public Guid RequestedByUserId { get; init; }
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string ScriptText { get; init; } = null!;
    public string AudioUrl { get; init; } = null!;
    public int DurationSeconds { get; init; }
    public string VoiceCode { get; init; } = null!;
    public string? VoiceDisplayName { get; init; }
    public string? AzuraCastMediaId { get; init; }
    public string Status { get; init; } = null!;
    public Guid? ReviewedByUserId { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectReason { get; init; }
    public DateTime RequestedAt { get; init; }

    // Joined data
    public string? SessionName { get; init; }
    public string? StationName { get; init; }
    public string? RequestedByUsername { get; init; }
    public string? ReviewedByUsername { get; init; }
}