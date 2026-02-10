using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.StartSession;

/// <summary>
/// Command to start a live session
/// </summary>
public sealed record StartSessionCommand(Guid SessionId) : ICommand<LiveSessionResult>;
