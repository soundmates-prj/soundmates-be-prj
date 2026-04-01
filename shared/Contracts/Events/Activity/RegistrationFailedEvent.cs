using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Activity;

/// <summary>
/// Published when a user registration attempt fails.
/// Routing key: auth.user.registration.failed
/// </summary>
public sealed class RegistrationFailedEvent : BaseIntegrationEvent
{
    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; init; } = string.Empty;
}
