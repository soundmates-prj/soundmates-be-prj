namespace AccountContentService.Application.Interfaces.Services;

/// <summary>
/// Provides cached user profile data for content write operations.
/// Reads from the local read-model (populated via RabbitMQ events).
/// Returns a default "pending" profile when the projection has not been
/// populated yet (eventual consistency gap).
/// </summary>
public interface IUserProfileCache
{
    /// <summary>
    /// Returns the user profile for the given userId.
    /// Returns a pending/default profile if not yet in the projection.
    /// </summary>
    Task<CachedUserProfile> GetProfileAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// DTO returned by IUserProfileCache.
/// </summary>
public sealed record CachedUserProfile(
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    bool IsPending);
