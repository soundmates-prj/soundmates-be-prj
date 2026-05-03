using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Commands.UpdateLiveSession;

public sealed record UpdateLiveSessionCommand(
    Guid SessionId,
    Guid UserId,
    Guid? HostUserId,
    Guid? StationId,
    string? SessionName,
    string? Description,
    string? ThumbnailUrl) : ICommand<LiveSessionResult>;
