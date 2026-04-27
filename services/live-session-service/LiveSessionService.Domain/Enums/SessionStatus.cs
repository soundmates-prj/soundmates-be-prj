namespace LiveSessionService.Domain.Enums;

public enum SessionStatus
{
    Created = 0,
    Scheduled = 1,
    Live = 2,
    Paused = 3,
    Ended = 4,
    Cancelled = 5
}
