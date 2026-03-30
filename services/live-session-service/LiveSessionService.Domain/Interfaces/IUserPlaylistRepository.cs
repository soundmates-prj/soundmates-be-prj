using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Interfaces;

public interface IUserPlaylistRepository
{
    Task<UserPlaylist?> GetByIdAsync(Guid playlistId, CancellationToken cancellationToken = default);
    Task<List<UserPlaylist>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserPlaylist playlist, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserPlaylist playlist, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserPlaylist playlist, CancellationToken cancellationToken = default);
    Task AddTracksAsync(IEnumerable<UserPlaylistMedia> tracks, CancellationToken cancellationToken = default);
    Task RemoveTracksAsync(IEnumerable<UserPlaylistMedia> tracks, CancellationToken cancellationToken = default);
    Task<List<UserPlaylistMedia>> GetTracksAsync(Guid playlistId, CancellationToken cancellationToken = default);
}
