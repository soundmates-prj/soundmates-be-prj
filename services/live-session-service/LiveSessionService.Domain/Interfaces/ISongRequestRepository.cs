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
    Task<int> CountRequestsByUserTodayAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SongRequest> Items, int TotalCount)> GetAllAsync(
        SongRequestStatus? status = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}
