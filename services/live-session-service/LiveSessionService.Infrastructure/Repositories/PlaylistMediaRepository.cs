using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class PlaylistMediaRepository : IPlaylistMediaRepository
{
    private readonly LiveSessionDbContext _context;

    public PlaylistMediaRepository(LiveSessionDbContext context)
    {
        _context = context;
    }

    public async Task<PlaylistMedia?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PlaylistMedias
            .Include(pm => pm.StationPlaylist)
            .Include(pm => pm.MediaFile)
            .FirstOrDefaultAsync(pm => pm.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PlaylistMedia>> GetByPlaylistIdAsync(
        Guid playlistId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.PlaylistMedias
            .Include(pm => pm.MediaFile)
            .Where(pm => pm.StationPlaylistId == playlistId)
            .OrderBy(pm => pm.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlaylistMedia>> GetByMediaFileIdAsync(
        Guid mediaFileId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PlaylistMedias
            .Where(pm => pm.MediaFileId == mediaFileId)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlaylistMedia?> GetByPlaylistAndMediaIdAsync(
        Guid playlistId, 
        string mediaId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.PlaylistMedias
            .FirstOrDefaultAsync(
                pm => pm.StationPlaylistId == playlistId && pm.MediaId == mediaId, 
                cancellationToken);
    }

    public async Task AddAsync(PlaylistMedia playlistMedia, CancellationToken cancellationToken = default)
    {
        await _context.PlaylistMedias.AddAsync(playlistMedia, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<PlaylistMedia> playlistMedias, 
        CancellationToken cancellationToken = default)
    {
        await _context.PlaylistMedias.AddRangeAsync(playlistMedias, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PlaylistMedia playlistMedia, CancellationToken cancellationToken = default)
    {
        _context.PlaylistMedias.Update(playlistMedia);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(PlaylistMedia playlistMedia, CancellationToken cancellationToken = default)
    {
        _context.PlaylistMedias.Remove(playlistMedia);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<PlaylistMedia> playlistMedias, CancellationToken cancellationToken = default)
    {
        var medias = playlistMedias.ToList();
        if (medias.Count == 0)
            return;

        _context.PlaylistMedias.RemoveRange(medias);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByPlaylistIdAsync(Guid playlistId, CancellationToken cancellationToken = default)
    {
        var playlistMedias = await _context.PlaylistMedias
            .Where(pm => pm.StationPlaylistId == playlistId)
            .ToListAsync(cancellationToken);

        if (playlistMedias.Count > 0)
        {
            _context.PlaylistMedias.RemoveRange(playlistMedias);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
