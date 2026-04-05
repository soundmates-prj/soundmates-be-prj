using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Activity;

/// <summary>
/// Published when a user logs in successfully.
/// Routing key: auth.user.login.successful
/// </summary>
public sealed class LoginSuccessfulEvent : BaseIntegrationEvent
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; init; }

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;
}
