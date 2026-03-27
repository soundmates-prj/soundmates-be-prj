using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AccountContentService.Infrastructure.Services;

public sealed class UserProfileCache : IUserProfileCache
{
    private readonly IUserProfileReadModelRepository _repo;
    private readonly ILogger<UserProfileCache> _logger;

    public UserProfileCache(
        IUserProfileReadModelRepository repo,
        ILogger<UserProfileCache> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<CachedUserProfile> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var projection = await _repo.GetByIdAsync(userId, ct);

        if (projection != null)
        {
            return new CachedUserProfile(
                UserId: projection.Id,
                FullName: projection.FullName,
                AvatarUrl: projection.AvatarUrl,
                IsPending: projection.IsPending);
        }

        // Projection not yet available — return a default pending profile.
        // The eventual-consistency gap means the user will appear with
        // "Unknown User" until the event is processed.
        _logger.LogDebug(
            "UserProfileReadModel not found for {UserId}, returning pending default",
            userId);

        return new CachedUserProfile(
            UserId: userId,
            FullName: "Unknown User",
            AvatarUrl: null,
            IsPending: true);
    }
}
