using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Interfaces;

public interface IPodcastEpisodeRequestRepository
{
    Task<PodcastEpisodeRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<PodcastEpisodeRequest>> GetAllAsync(PodcastRequestStatus? status, string? search, CancellationToken cancellationToken = default);
    Task<List<PodcastEpisodeRequest>> GetByUserIdAsync(Guid userId, PodcastRequestStatus? status, CancellationToken cancellationToken = default);
    Task<PodcastEpisodeRequest> AddAsync(PodcastEpisodeRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(PodcastEpisodeRequest request, CancellationToken cancellationToken = default);
}
