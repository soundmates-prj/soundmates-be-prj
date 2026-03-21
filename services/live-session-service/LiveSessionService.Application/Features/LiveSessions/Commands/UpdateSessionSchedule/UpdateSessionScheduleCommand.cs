using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.UpdateSessionSchedule;

public sealed record UpdateSessionScheduleCommand(
    Guid ScheduleId,
    DateTime StartTime,
    DateTime EndTime,
    string? Title,
    string? Status,
    Guid ActorUserId) : ICommand<SessionScheduleResult>;
