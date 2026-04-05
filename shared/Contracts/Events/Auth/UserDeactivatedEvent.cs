using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Auth;

/// <summary>
/// Published when a user account is deactivated (soft deactivation by user choice).
/// Routing key: auth.user.deactivated
/// </summary>
public sealed class UserDeactivatedEvent : BaseIntegrationEvent
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; init; }

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("deactivatedAt")]
    public DateTime DeactivatedAt { get; init; }
}
