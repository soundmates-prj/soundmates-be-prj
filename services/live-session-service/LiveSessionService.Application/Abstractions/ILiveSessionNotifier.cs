using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Abstractions;

/// <summary>
/// Abstraction for broadcasting live session events via SignalR
/// Implemented in the Api layer to avoid referencing SignalR from Application
/// </summary>
public interface ILiveSessionNotifier
{
    Task NotifySessionStarted(LiveSessionResult session, CancellationToken cancellationToken = default);
    Task NotifySessionEnded(LiveSessionResult session, CancellationToken cancellationToken = default);
}
