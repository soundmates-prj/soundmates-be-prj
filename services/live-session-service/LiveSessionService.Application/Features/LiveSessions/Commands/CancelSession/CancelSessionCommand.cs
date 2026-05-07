using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.CancelSession;

public sealed record CancelSessionCommand(Guid SessionId, Guid ActorUserId) : ICommand<LiveSessionResult>;
