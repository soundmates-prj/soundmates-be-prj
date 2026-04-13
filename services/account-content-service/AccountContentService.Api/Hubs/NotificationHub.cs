using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AccountContentService.Api.Hubs;

/// <summary>
/// SignalR hub for real-time notification push to clients.
/// Clients connect and automatically join their user-specific group.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            // Add user to their personal notification group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}
