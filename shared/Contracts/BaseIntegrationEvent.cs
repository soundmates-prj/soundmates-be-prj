using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts;

/// <summary>
/// Base class for all integration events published across services.
/// Provides a consistent envelope including event identity and occurred-on timestamp.
/// </summary>
public abstract class BaseIntegrationEvent
{
    /// <summary>Unique identifier for this event instance.</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>UTC timestamp when the event occurred.</summary>
    [JsonPropertyName("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;

    /// <summary>User or system context that triggered this event.</summary>
    [JsonPropertyName("correlationId")]
    public Guid? CorrelationId { get; init; }
}
