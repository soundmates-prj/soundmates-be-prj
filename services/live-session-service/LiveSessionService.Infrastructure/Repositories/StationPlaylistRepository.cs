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

    public Task<List<StationPlaylist>> GetByStationIdAsync(Guid stationId, CancellationToken cancellationToken = default)
        => _db.StationPlaylists
              .Include(p => p.Media)
              .Where(p => p.AzuraCastStationId == stationId && p.IsEnabled)
              .OrderBy(p => p.PlaylistOrder)
              .ToListAsync(cancellationToken);

    public async Task AddAsync(StationPlaylist playlist, CancellationToken cancellationToken = default)
    {
        await _db.StationPlaylists.AddAsync(playlist, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(StationPlaylist playlist, CancellationToken cancellationToken = default)
    {
        _db.StationPlaylists.Update(playlist);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMediaAsync(PlaylistMedia media, CancellationToken cancellationToken = default)
    {
        await _db.PlaylistMedias.AddAsync(media, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
