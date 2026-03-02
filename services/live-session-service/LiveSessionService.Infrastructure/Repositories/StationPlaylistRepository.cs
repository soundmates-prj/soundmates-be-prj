using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class StationPlaylistRepository : IStationPlaylistRepository
{
    private readonly LiveSessionDbContext _db;

    public StationPlaylistRepository(LiveSessionDbContext db) => _db = db;

    public Task<StationPlaylist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.StationPlaylists
              .Include(p => p.AzuraCastStation)
              .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task AddAsync(StationPlaylist playlist, CancellationToken cancellationToken = default)
    {
        await _db.StationPlaylists.AddAsync(playlist, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMediaAsync(PlaylistMedia media, CancellationToken cancellationToken = default)
    {
        await _db.PlaylistMedias.AddAsync(media, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
