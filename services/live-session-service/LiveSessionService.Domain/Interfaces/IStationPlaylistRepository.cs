using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Interfaces;

public interface IStationPlaylistRepository
{
    Task<StationPlaylist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<StationPlaylist>> GetByStationIdAsync(Guid stationId, CancellationToken cancellationToken = default);
    Task AddAsync(StationPlaylist playlist, CancellationToken cancellationToken = default);
    Task UpdateAsync(StationPlaylist playlist, CancellationToken cancellationToken = default);
    Task AddMediaAsync(PlaylistMedia media, CancellationToken cancellationToken = default);
}
