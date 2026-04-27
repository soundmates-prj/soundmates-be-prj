using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.UpdateSessionSchedule;

public sealed record UpdateSessionScheduleCommand(
    Guid ScheduleId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Title,
    Guid ActorUserId,
    bool? IsRecurring,
    DaysOfWeek? DaysOfWeek) : ICommand<SessionScheduleResult>;
