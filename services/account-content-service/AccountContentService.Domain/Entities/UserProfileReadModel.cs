namespace AccountContentService.Domain.Entities;

/// <summary>
/// Local projection of user profile for content write operations.
/// Updated asynchronously via RabbitMQ events from auth-service.
/// </summary>
public class UserProfileReadModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name built from FirstName + LastName.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Avatar / profile picture URL.
    /// </summary>
    public string? AvatarUrl { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Set to true when a write operation hit this record before the
    /// projection was populated (eventual consistency gap).
    /// </summary>
    public bool IsPending { get; set; }

    /// <summary>
    /// UTC timestamp of the last event that updated this record.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp when this record was first created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
