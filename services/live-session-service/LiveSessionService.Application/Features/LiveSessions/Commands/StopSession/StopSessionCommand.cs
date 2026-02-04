using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.StopSession;

public sealed record StopSessionCommand(Guid SessionId) : ICommand<LiveSessionResult>;
