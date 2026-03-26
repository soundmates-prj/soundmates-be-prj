using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Interfaces;

public interface ISessionScheduleRepository
{
    Task AddAsync(SessionSchedule schedule, CancellationToken cancellationToken = default);
    Task<List<SessionSchedule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SessionSchedule?> GetByIdAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<List<SessionSchedule>> GetByLiveSessionIdAsync(Guid liveSessionId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, List<SessionSchedule>>> GetByLiveSessionIdsAsync(
        IReadOnlyCollection<Guid> liveSessionIds,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(SessionSchedule schedule, CancellationToken cancellationToken = default);
    Task DeleteAsync(SessionSchedule schedule, CancellationToken cancellationToken = default);
    Task<SessionSchedule?> GetLatestByLiveSessionIdAsync(Guid liveSessionId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, SessionSchedule>> GetLatestByLiveSessionIdsAsync(
        IReadOnlyCollection<Guid> liveSessionIds,
        CancellationToken cancellationToken = default);
}
