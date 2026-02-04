using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.CreateLiveSession;

/// <summary>
/// Command to create a new live session
/// </summary>
public sealed record CreateLiveSessionCommand(
    Guid UserId,
    Guid StationId,
    string SessionName,
    string? Description) : ICommand<LiveSessionResult>;
