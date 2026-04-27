using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using LiveSessionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Infrastructure.Repositories;

public sealed class StationMediaFileRepository : IStationMediaFileRepository
{
    private readonly LiveSessionDbContext _db;
    private readonly ILogger<StationMediaFileRepository> _logger;

    public StationMediaFileRepository(LiveSessionDbContext db, ILogger<StationMediaFileRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<StationMediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.StationMediaFiles
            .Include(x => x.MediaFile)
            .Include(x => x.Station)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<StationMediaFile>> GetByStationIdAsync(Guid stationId, CancellationToken cancellationToken = default)
        => await _db.StationMediaFiles
            .Include(x => x.MediaFile)
            .Where(x => x.StationId == stationId)
            .OrderByDescending(x => x.ImportedAt)
            .ToListAsync(cancellationToken);

    public async Task<List<StationMediaFile>> GetByMediaFileIdAsync(Guid mediaFileId, CancellationToken cancellationToken = default)
        => await _db.StationMediaFiles
            .Include(x => x.Station)
            .Where(x => x.MediaFileId == mediaFileId)
            .OrderByDescending(x => x.ImportedAt)
            .ToListAsync(cancellationToken);

    public Task<StationMediaFile?> GetByMediaFileAndStationAsync(Guid mediaFileId, Guid stationId, CancellationToken cancellationToken = default)
        => _db.StationMediaFiles
            .FirstOrDefaultAsync(x => x.MediaFileId == mediaFileId && x.StationId == stationId, cancellationToken);

    public async Task AddAsync(StationMediaFile stationMediaFile, CancellationToken cancellationToken = default)
    {
        // Check if already exists
        var existing = await GetByMediaFileAndStationAsync(
            stationMediaFile.MediaFileId,
            stationMediaFile.StationId,
            cancellationToken);

        if (existing != null)
        {
            _logger.LogWarning(
                "StationMediaFile already exists for MediaFileId={MediaFileId} and StationId={StationId}. Skipping.",
                stationMediaFile.MediaFileId,
                stationMediaFile.StationId);
            return;
        }

        await _db.StationMediaFiles.AddAsync(stationMediaFile, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(StationMediaFile stationMediaFile, CancellationToken cancellationToken = default)
    {
        _db.StationMediaFiles.Remove(stationMediaFile);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
