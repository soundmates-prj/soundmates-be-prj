using AccountContentService.Domain.Entities;

namespace AccountContentService.Application.Interfaces;

/// <summary>
/// Abstraction for pushing real-time notifications via SignalR.
/// Implemented in the Api layer to avoid referencing SignalR from Application.
/// </summary>
public interface INotificationPusher
{
    /// <summary>
    /// Push a notification to a specific user in real-time.
    /// </summary>
    Task PushToUserAsync(Guid userId, Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast a notification payload to ALL connected clients in real-time.
    /// The notification is NOT persisted per-user by this method.
    Task PushToAllAsync(object payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Push a notification deletion event to a specific user in real-time.
    /// </summary>
    Task PushDeleteToUserAsync(Guid userId, Guid referenceId, string type, CancellationToken cancellationToken = default);
}
