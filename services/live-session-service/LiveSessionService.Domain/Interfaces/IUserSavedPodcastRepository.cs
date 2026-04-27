using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Interfaces;

public interface IUserSavedPodcastRepository
{
    Task<UserSavedPodcast?> GetByUserAndPodcastAsync(Guid userId, Guid podcastId, CancellationToken cancellationToken = default);
    Task<List<UserSavedPodcast>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserSavedPodcast userSavedPodcast, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserSavedPodcast userSavedPodcast, CancellationToken cancellationToken = default);
}
