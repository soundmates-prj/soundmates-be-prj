using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Activity;

/// <summary>
/// Published when a user login attempt fails.
/// Routing key: auth.user.login.failed
/// </summary>
public sealed class LoginFailedEvent : BaseIntegrationEvent
{
    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }

    [JsonPropertyName("userId")]
    public Guid? UserId { get; init; }

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; init; }
}
