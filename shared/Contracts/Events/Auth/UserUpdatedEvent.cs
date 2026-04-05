using System;
using System.Text.Json.Serialization;

namespace Shared.Contracts.Events.Auth;

/// <summary>
/// Published when user basic info is updated (username, email, name, role).
/// Routing key: auth.user.updated
/// </summary>
public sealed class UserUpdatedEvent : BaseIntegrationEvent
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
    public string? RoleName { get; init; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}
