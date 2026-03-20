using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Interfaces;

/// <summary>
/// Repository interface for LiveSession entity
/// Follows Repository pattern for data access abstraction
/// </summary>
public interface ILiveSessionRepository
{
    Task<LiveSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<LiveSession>> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
    Task<List<LiveSession>> GetByHostUserIdAsync(Guid hostUserId, CancellationToken cancellationToken = default);
    Task<LiveSession?> GetByIdWithStationAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<LiveSession>> GetAllWithStationsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(LiveSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(LiveSession session, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetActiveSessionCountAsync(CancellationToken cancellationToken = default);
    Task EndSessionCleanupAsync(Guid sessionId, DateTime endedAt, CancellationToken cancellationToken = default);
}
