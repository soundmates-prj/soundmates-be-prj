namespace LiveSessionService.Application.Features.Results.LiveSessions;

/// <summary>
/// Result model for live session operations
/// </summary>
public sealed class LiveSessionResult
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid StationId { get; init; }
    public string? StationName { get; init; }
    public string SessionName { get; init; } = null!;
    public string? Description { get; init; }
    public string Status { get; init; } = null!;
    public DateTime? ScheduledStartAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public int TotalListeners { get; init; }
    public int PeakListeners { get; init; }
    public int TotalDuration { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? StreamUrl { get; init; }
    public string? StationShortcode { get; init; }
    public string? PublicPlayerUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string? Genre { get; init; }
    public int ListenersCount { get; init; }
}
