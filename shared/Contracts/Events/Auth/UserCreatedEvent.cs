using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Auth;

/// <summary>
/// Published when a new user is registered or created via Google OAuth.
/// Routing key: auth.user.created
/// </summary>
public sealed class UserCreatedEvent : BaseIntegrationEvent
{
    [JsonPropertyName("id")]
    public new Guid Id { get; init; }

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; init; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; init; } = string.Empty;

    [JsonPropertyName("roleId")]
    public Guid RoleId { get; init; }

    [JsonPropertyName("roleName")]
    public string RoleName { get; init; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    /// <summary>
    /// Canonical account status. 1=Active, 2=Deactivated, 3=Suspended, 4=PendingDeletion
    /// </summary>
    [JsonPropertyName("accountStatus")]
    public int AccountStatus { get; init; } = 1;

    [JsonPropertyName("isVerified")]
    public bool IsVerified { get; init; }

    [JsonPropertyName("emailVerifiedAt")]
    public DateTime? EmailVerifiedAt { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }
}
