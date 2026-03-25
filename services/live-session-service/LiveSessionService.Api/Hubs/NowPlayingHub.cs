using Microsoft.AspNetCore.SignalR;

namespace LiveSessionService.Api.Hubs;

/// <summary>
/// SignalR hub for real-time now-playing updates.
/// Clients join a station group to receive push notifications when the song changes.
///
/// Client usage (JavaScript):
///   const conn = new signalR.HubConnectionBuilder().withUrl("/hubs/now-playing").build();
///   await conn.start();
///   await conn.invoke("JoinStation", "{station-guid}");
///   conn.on("NowPlayingUpdated", data => console.log(data));
/// </summary>
public sealed class NowPlayingHub : Hub
{
    public async Task JoinStation(string stationId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"station-{stationId}");

    public async Task LeaveStation(string stationId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"station-{stationId}");

    public async Task JoinSession(string sessionId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");

    public async Task LeaveSession(string sessionId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session-{sessionId}");
}
