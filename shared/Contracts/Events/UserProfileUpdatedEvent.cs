using System.Text.Json.Serialization;

namespace Shared.Contracts.Events;

/// <summary>
/// Published by auth-service when user basic info or extended profile is updated.
/// Routing key: auth.user.profile.updated
/// </summary>
public sealed class UserProfileUpdatedEvent
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

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

    [JsonPropertyName("profile")]
    public UserProfileDetail? Profile { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}

public sealed class UserProfileDetail
{
    [JsonPropertyName("bio")]
    public string? Bio { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("gender")]
    public string? Gender { get; init; }

    [JsonPropertyName("dateOfBirth")]
    public DateTime? DateOfBirth { get; init; }

    [JsonPropertyName("profileImageUrl")]
    public string? ProfileImageUrl { get; init; }

    [JsonPropertyName("backgroundImageUrl")]
    public string? BackgroundImageUrl { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("website")]
    public string? Website { get; init; }
}
