namespace LiveSessionService.Domain.Interfaces;

/// <summary>
/// Outbox pattern interface for reliable event publishing
/// Messages are saved in same transaction as domain changes
/// </summary>
public interface IOutboxRepository
{
    Task EnqueueAsync(string eventType, object payload, CancellationToken cancellationToken = default);
}
