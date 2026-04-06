using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Auth;

/// <summary>
/// Published when a user account is banned by an admin.
/// Routing key: auth.user.banned
/// </summary>
public sealed class UserBannedEvent : BaseIntegrationEvent
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; init; }

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("bannedAt")]
    public DateTime BannedAt { get; init; }
}
