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
}
