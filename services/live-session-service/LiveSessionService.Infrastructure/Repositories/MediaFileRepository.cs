using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class MediaFileRepository : IMediaFileRepository
{
    private readonly LiveSessionDbContext _db;
    private readonly ILogger<MediaFileRepository> _logger;

    public MediaFileRepository(LiveSessionDbContext db, ILogger<MediaFileRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.MediaFiles.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MediaFile>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<MediaFile>();
        }

        return await _db.MediaFiles
            .Where(f => ids.Contains(f.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<MediaFile?> GetByFilePathAsync(string filePath, CancellationToken cancellationToken = default)
        => _db.MediaFiles.FirstOrDefaultAsync(
            f => f.FilePath == filePath || f.AzuraCastMediaId == filePath,
            cancellationToken);

    public Task<MediaFile?> GetByAzuraCastMediaIdAsync(string azuraCastMediaId, CancellationToken cancellationToken = default)
        => _db.MediaFiles.FirstOrDefaultAsync(
            f => f.AzuraCastMediaId == azuraCastMediaId,
            cancellationToken);

    public async Task<IReadOnlyList<MediaFile>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.MediaFiles
            .AsNoTracking()
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MediaFile>> GetByFilePathsAsync(IReadOnlyCollection<string> filePaths, CancellationToken cancellationToken = default)
    {
        if (filePaths.Count == 0)
        {
            return Array.Empty<MediaFile>();
        }

        return await _db.MediaFiles
            .AsNoTracking()
            .Where(f => filePaths.Contains(f.FilePath))
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MediaFile>> GetByAzuraCastMediaIdsAsync(IReadOnlyCollection<string> azuraCastMediaIds, CancellationToken cancellationToken = default)
    {
        if (azuraCastMediaIds.Count == 0)
        {
            return Array.Empty<MediaFile>();
        }

        return await _db.MediaFiles
            .AsNoTracking()
            .Where(f => f.AzuraCastMediaId != null && azuraCastMediaIds.Contains(f.AzuraCastMediaId))
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MediaFile>> GetByStationIdAsync(Guid stationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "MediaFileRepository: Querying media files by StationId={StationId} through PlaylistMedia -> StationPlaylist",
            stationId);

        var result = await _db.MediaFiles
            .AsNoTracking()
            .Where(f => _db.PlaylistMedias.Any(pm =>
                pm.MediaFileId == f.Id &&
                pm.StationPlaylist.AzuraCastStationId == stationId))
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "MediaFileRepository: Found {Count} media files for StationId={StationId}",
            result.Count,
            stationId);

        return result;
    }

    public async Task AddAsync(MediaFile mediaFile, CancellationToken cancellationToken = default)
    {
        await _db.MediaFiles.AddAsync(mediaFile, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MediaFile mediaFile, CancellationToken cancellationToken = default)
    {
        _db.MediaFiles.Update(mediaFile);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(MediaFile mediaFile, CancellationToken cancellationToken = default)
    {
        _db.MediaFiles.Remove(mediaFile);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
