using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Application.Features.Results.PodcastRequests;

namespace LiveSessionService.Application.Abstractions;

/// <summary>
/// Abstraction for broadcasting live session events via SignalR
/// Implemented in the Api layer to avoid referencing SignalR from Application
/// </summary>
public interface ILiveSessionNotifier
{
    Task NotifySessionStarted(LiveSessionResult session, CancellationToken cancellationToken = default);
    Task NotifySessionEnded(LiveSessionResult session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts when a new podcast request is submitted to a session.
    /// Sent to all connected clients in the session group (listeners, host, staff).
    /// </summary>
    Task NotifyPodcastRequestCreated(PodcastRequestResult request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts when a podcast request is reviewed (approved/rejected).
    /// Sent to the requester and all connected clients in the session group.
    /// </summary>
    Task NotifyPodcastRequestReviewed(PodcastRequestResult request, CancellationToken cancellationToken = default);
}
