using AccountContentService.Api.Hubs;
using AccountContentService.Application.Interfaces;
using AccountContentService.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace AccountContentService.Api.Services;

/// <summary>
/// SignalR implementation of INotificationPusher.
/// Pushes notifications to connected clients in real-time.
/// </summary>
public sealed class NotificationPusher : INotificationPusher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationPusher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PushToUserAsync(Guid userId, Notification notification, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.UserId,
                notification.Title,
                notification.Message,
                notification.Type,
                notification.ReferenceId,
                notification.IsRead,
                notification.CreatedAt
            }, cancellationToken);
    }
}
