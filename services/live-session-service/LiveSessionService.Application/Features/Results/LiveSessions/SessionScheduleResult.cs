using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Application.Features.Results.LiveSessions;

public sealed class SessionScheduleResult
{
    public Guid Id { get; init; }
    public Guid LiveSessionId { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public string? Title { get; init; }
    public string? Status { get; init; }
    public bool IsRecurring { get; init; }
    public DaysOfWeek DaysOfWeek { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public Guid? CreatedBy { get; init; }
    public Guid? UpdatedBy { get; init; }

    /// <summary>
    /// UTC timestamp when the schedule was created
    /// </summary>
    public DateTime CreatedAt { get; init; }

    // Backward-compatible aliases for newer API contract naming
    public Guid? CreatedByUserId { get; init; }
    public Guid? UpdatedByUserId { get; init; }

    /// <summary>
    /// Full live session data including station info
    /// </summary>
    public LiveSessionScheduleData? LiveSession { get; init; }
}

public sealed class LiveSessionScheduleData
{
    public Guid Id { get; init; }
    public string SessionName { get; init; } = null!;
    public string? Description { get; init; }
    public string Status { get; init; } = null!;
    public Guid HostUserId { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public string? Genre { get; init; }
    public string? ThumbnailUrl { get; init; }

    /// <summary>
    /// Station data for thumbnail/stream
    /// </summary>
    public StationScheduleData? Station { get; init; }
}

public sealed class StationScheduleData
{
    public Guid Id { get; init; }
    public int ExternalStationId { get; init; }
    public string StationName { get; init; } = null!;
    public string? StationShortcode { get; init; }
    public string? Description { get; init; }
    public string? StreamUrl { get; init; }
    public string? PublicPlayerUrl { get; init; }
}
