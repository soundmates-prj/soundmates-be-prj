using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.CreateSessionSchedule;

public sealed record CreateSessionScheduleCommand(
    Guid LiveSessionId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Title,
    Guid ActorUserId,
    bool IsRecurring,
    DaysOfWeek DaysOfWeek) : ICommand<SessionScheduleResult>;
