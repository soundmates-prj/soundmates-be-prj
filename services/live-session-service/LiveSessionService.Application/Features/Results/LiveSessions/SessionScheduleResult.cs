namespace LiveSessionService.Application.Features.Results.LiveSessions;

public sealed class SessionScheduleResult
{
    public Guid Id { get; init; }
    public Guid LiveSessionId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string Title { get; init; } = null!;
    public string? Status { get; init; }
}
