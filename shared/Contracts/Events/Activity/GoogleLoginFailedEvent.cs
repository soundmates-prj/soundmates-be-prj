using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Activity;

/// <summary>
/// Published when a Google OAuth login attempt fails.
/// Routing key: auth.user.google.login.failed
/// </summary>
public sealed class GoogleLoginFailedEvent : BaseIntegrationEvent
{
    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; init; }
}
