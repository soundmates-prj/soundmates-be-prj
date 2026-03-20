using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Interfaces;

public interface ISessionScheduleRepository
{
    Task AddAsync(SessionSchedule schedule, CancellationToken cancellationToken = default);
    Task<List<SessionSchedule>> GetByLiveSessionIdAsync(Guid liveSessionId, CancellationToken cancellationToken = default);
    Task<SessionSchedule?> GetLatestByLiveSessionIdAsync(Guid liveSessionId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, SessionSchedule>> GetLatestByLiveSessionIdsAsync(
        IReadOnlyCollection<Guid> liveSessionIds,
        CancellationToken cancellationToken = default);
}
