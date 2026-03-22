using LiveSessionService.Application.Abstractions.Messaging;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.DeleteSessionSchedule;

public sealed record DeleteSessionScheduleCommand(Guid ScheduleId, Guid ActorUserId) : ICommand;
