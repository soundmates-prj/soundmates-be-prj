using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;

namespace LiveSessionService.Domain.Interfaces;

public interface ISongRequestRepository
{
    Task<SongRequest?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SongRequest>> GetByLiveSessionIdAsync(
        Guid liveSessionId,
        SongRequestStatus? status = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(SongRequest songRequest, CancellationToken cancellationToken = default);
    Task UpdateAsync(SongRequest songRequest, CancellationToken cancellationToken = default);
}
