using System.Text.Json.Serialization;

namespace Shared.Contracts.Events;

/// <summary>
/// Published by auth-service when a user account is deleted/deactivated.
/// Routing key: auth.user.deleted
/// </summary>
public sealed class UserDeletedEvent
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("deletedAt")]
    public DateTime DeletedAt { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}
