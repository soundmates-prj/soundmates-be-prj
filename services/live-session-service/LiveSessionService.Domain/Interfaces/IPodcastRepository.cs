using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Interfaces;

public interface IPodcastRepository
{
    Task<Podcast?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Podcast>> GetAllAsync(
        Guid? createdBy = null,
        PodcastStatus? status = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Podcast podcast, CancellationToken cancellationToken = default);
    Task UpdateAsync(Podcast podcast, CancellationToken cancellationToken = default);
    Task DeleteAsync(Podcast podcast, CancellationToken cancellationToken = default);

    Task<PodcastEpisode?> GetEpisodeByIdAsync(Guid episodeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PodcastEpisode>> GetEpisodesByPodcastIdAsync(Guid podcastId, CancellationToken cancellationToken = default);
    Task<PodcastEpisode?> GetEpisodeByNumberAsync(Guid podcastId, int episodeNumber, CancellationToken cancellationToken = default);
    Task AddEpisodeAsync(PodcastEpisode episode, CancellationToken cancellationToken = default);
    Task UpdateEpisodeAsync(PodcastEpisode episode, CancellationToken cancellationToken = default);
    Task DeleteEpisodeAsync(PodcastEpisode episode, CancellationToken cancellationToken = default);
}
