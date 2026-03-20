using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.ResumeSession;

public sealed record ResumeSessionCommand(Guid SessionId) : ICommand<LiveSessionResult>;
