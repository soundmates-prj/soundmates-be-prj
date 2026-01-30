using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Interfaces;

/// <summary>
/// Repository interface for NowPlayingHistory entity
/// </summary>
public interface INowPlayingHistoryRepository
{
    Task<NowPlayingHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<NowPlayingHistory?> GetLatestBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<List<NowPlayingHistory>> GetBySessionIdAsync(Guid sessionId, int limit = 50, CancellationToken cancellationToken = default);
    Task AddAsync(NowPlayingHistory history, CancellationToken cancellationToken = default);
    Task UpdateAsync(NowPlayingHistory history, CancellationToken cancellationToken = default);
}
