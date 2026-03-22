using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.CreateSessionSchedule;

public sealed record CreateSessionScheduleCommand(
    Guid LiveSessionId,
    DateTime StartTime,
    DateTime EndTime,
    string? Title,
    Guid ActorUserId) : ICommand<SessionScheduleResult>;
