using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Activity;

/// <summary>
/// Published when a Google OAuth login completes successfully.
/// Routing key: auth.user.google.login.successful
/// </summary>
public sealed class GoogleLoginSuccessfulEvent : BaseIntegrationEvent
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;
}
