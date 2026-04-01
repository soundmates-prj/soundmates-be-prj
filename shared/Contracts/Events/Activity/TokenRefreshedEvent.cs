using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Activity;

/// <summary>
/// Published when a user refreshes their access token.
/// Routing key: auth.user.token.refreshed
/// </summary>
public sealed class TokenRefreshedEvent : BaseIntegrationEvent
{
    [JsonPropertyName("userId")]
    public Guid UserId { get; init; }
}
