using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Interfaces;

public interface IPodcastRequestRepository
{
    Task<PodcastRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PodcastRequest>> GetAllAsync(
        PodcastRequestStatus? status = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(PodcastRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(PodcastRequest request, CancellationToken cancellationToken = default);
}