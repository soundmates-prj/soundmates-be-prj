using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Auth;

/// <summary>
/// Published when a user account is deleted (hard delete).
/// Routing key: auth.user.deleted
/// </summary>
public sealed class UserDeletedEvent : BaseIntegrationEvent
{
    [JsonPropertyName("id")]
    public new Guid Id { get; init; }

    [JsonPropertyName("deletedAt")]
    public DateTime DeletedAt { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}
