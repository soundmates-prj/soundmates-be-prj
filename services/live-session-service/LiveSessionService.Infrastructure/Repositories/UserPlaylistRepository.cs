using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class UserPlaylistRepository : IUserPlaylistRepository
{
    private readonly LiveSessionDbContext _db;

    public UserPlaylistRepository(LiveSessionDbContext db)
    {
        _db = db;
    }

    public Task<UserPlaylist?> GetByIdAsync(Guid playlistId, CancellationToken cancellationToken = default)
        => _db.UserPlaylists
            .Include(x => x.UserPlaylistMedias)
                .ThenInclude(x => x.MediaFile)
            .FirstOrDefaultAsync(x => x.Id == playlistId, cancellationToken);

    public Task<List<UserPlaylist>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _db.UserPlaylists
            .Include(x => x.UserPlaylistMedias)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.PlaylistOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<List<UserPlaylist>> GetAllPublicAsync(CancellationToken cancellationToken = default)
        => _db.UserPlaylists
            .Include(x => x.UserPlaylistMedias)
            .Where(x => x.Visibility == LiveSessionService.Domain.Enums.PlaylistVisibility.Public && x.IsEnabled)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(UserPlaylist playlist, CancellationToken cancellationToken = default)
    {
        await _db.UserPlaylists.AddAsync(playlist, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserPlaylist playlist, CancellationToken cancellationToken = default)
    {
        _db.UserPlaylists.Update(playlist);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(UserPlaylist playlist, CancellationToken cancellationToken = default)
    {
        _db.UserPlaylists.Remove(playlist);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddTracksAsync(IEnumerable<UserPlaylistMedia> tracks, CancellationToken cancellationToken = default)
    {
        await _db.UserPlaylistMedias.AddRangeAsync(tracks, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveTracksAsync(IEnumerable<UserPlaylistMedia> tracks, CancellationToken cancellationToken = default)
    {
        _db.UserPlaylistMedias.RemoveRange(tracks);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<List<UserPlaylistMedia>> GetTracksAsync(Guid playlistId, CancellationToken cancellationToken = default)
        => _db.UserPlaylistMedias
            .Include(x => x.MediaFile)
            .Where(x => x.UserPlaylistId == playlistId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<List<UserPlaylist>> GetPublicPlaylistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _db.UserPlaylists
            .Include(x => x.UserPlaylistMedias)
            .Where(x => x.UserId == userId && x.Visibility == LiveSessionService.Domain.Enums.PlaylistVisibility.Public && x.IsEnabled)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
}
