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
}
