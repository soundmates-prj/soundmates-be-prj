using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.PauseSession;

public sealed record PauseSessionCommand(Guid SessionId) : ICommand<LiveSessionResult>;
