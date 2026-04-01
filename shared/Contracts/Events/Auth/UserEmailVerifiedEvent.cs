using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Auth;

/// <summary>
/// Published when a user's email is verified (via OTP or admin action).
/// Routing key: auth.user.email.verified
/// </summary>
public sealed class UserEmailVerifiedEvent : BaseIntegrationEvent
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; init; }

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("emailVerifiedAt")]
    public DateTime EmailVerifiedAt { get; init; }
}
