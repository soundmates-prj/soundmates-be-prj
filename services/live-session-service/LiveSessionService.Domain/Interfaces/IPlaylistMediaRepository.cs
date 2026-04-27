using LiveSessionService.Domain.Entities;

namespace LiveSessionService.Domain.Interfaces;

public interface IPlaylistMediaRepository
{
    Task<PlaylistMedia?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlaylistMedia>> GetByPlaylistIdAsync(Guid playlistId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlaylistMedia>> GetByMediaFileIdAsync(Guid mediaFileId, CancellationToken cancellationToken = default);
    Task<PlaylistMedia?> GetByPlaylistAndMediaIdAsync(Guid playlistId, string mediaId, CancellationToken cancellationToken = default);
    Task AddAsync(PlaylistMedia playlistMedia, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<PlaylistMedia> playlistMedias, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlaylistMedia playlistMedia, CancellationToken cancellationToken = default);
    Task DeleteAsync(PlaylistMedia playlistMedia, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<PlaylistMedia> playlistMedias, CancellationToken cancellationToken = default);
    Task DeleteByPlaylistIdAsync(Guid playlistId, CancellationToken cancellationToken = default);
}
