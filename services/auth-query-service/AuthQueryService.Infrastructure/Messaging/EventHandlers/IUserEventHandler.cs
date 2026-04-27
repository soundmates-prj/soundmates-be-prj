using System.Text.Json;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers
{
    /// <summary>
    /// Interface for handling user-related events
    /// </summary>
    public interface IUserEventHandler
    {
        /// <summary>
        /// Event type this handler processes (e.g., "auth.user.created")
        /// </summary>
        string EventType { get; }

        /// <summary>
        /// Handle the event
        /// </summary>
        Task HandleAsync(JsonDocument eventData, CancellationToken cancellationToken);
    }
}
