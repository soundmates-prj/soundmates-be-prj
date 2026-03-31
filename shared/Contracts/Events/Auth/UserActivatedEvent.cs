using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Auth;

/// <summary>
/// Published when a user account is activated.
/// Routing key: auth.user.activated
/// </summary>
public sealed class UserActivatedEvent : BaseIntegrationEvent
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; init; }

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;
}
