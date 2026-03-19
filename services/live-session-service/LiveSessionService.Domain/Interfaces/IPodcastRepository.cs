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
}
