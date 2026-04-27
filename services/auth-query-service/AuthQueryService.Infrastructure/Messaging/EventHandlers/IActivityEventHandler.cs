using System.Text.Json;

namespace AuthQueryService.Infrastructure.Messaging.EventHandlers
{
    /// <summary>
    /// Interface for handling activity/security-related events
    /// </summary>
    public interface IActivityEventHandler
    {
        /// <summary>
        /// Event type this handler processes (e.g., "auth.user.login.successful")
        /// </summary>
        string EventType { get; }

        /// <summary>
        /// Handle the activity event
        /// </summary>
        Task HandleAsync(JsonDocument eventData, CancellationToken cancellationToken);
    }
}
