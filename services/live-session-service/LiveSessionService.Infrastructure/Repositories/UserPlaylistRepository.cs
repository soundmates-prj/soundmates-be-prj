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
            .FirstOrDefaultAsync(x => x.Id == playlistId, cancellationToken);

    public Task<List<UserPlaylist>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _db.UserPlaylists
            .Include(x => x.UserPlaylistMedias)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.PlaylistOrder)
            .ThenByDescending(x => x.CreatedAt)
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
}
