using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Features.Results.LiveSessions;
using Microsoft.AspNetCore.SignalR;

namespace LiveSessionService.Api.Hubs;

/// <summary>
/// SignalR implementation of ILiveSessionNotifier
/// Broadcasts session lifecycle events to all connected clients
/// </summary>
public sealed class LiveSessionNotifier : ILiveSessionNotifier
{
    private readonly IHubContext<LiveSessionHub> _hubContext;

    public LiveSessionNotifier(IHubContext<LiveSessionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifySessionStarted(LiveSessionResult session, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("SessionStarted", session, cancellationToken);
    }

    public async Task NotifySessionEnded(LiveSessionResult session, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("SessionEnded", session, cancellationToken);
    }
}
