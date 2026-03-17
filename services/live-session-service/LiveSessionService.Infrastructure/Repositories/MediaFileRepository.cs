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

    public Task<MediaFile?> GetByFilePathAsync(string filePath, CancellationToken cancellationToken = default)
        => _db.MediaFiles.FirstOrDefaultAsync(f => f.FilePath == filePath, cancellationToken);


    public async Task<IReadOnlyList<MediaFile>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.MediaFiles
            .AsNoTracking()
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MediaFile>> GetByStationIdAsync(Guid stationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MediaFileRepository: Querying database for StationId={StationId}", stationId);
        
        var allFiles = await _db.MediaFiles.AsNoTracking().ToListAsync(cancellationToken);
        _logger.LogInformation("MediaFileRepository: Total media files in database: {Count}", allFiles.Count);
        
        if (allFiles.Any())
        {
            var stationIds = allFiles.Select(f => f.StationId).Distinct().ToList();
            _logger.LogInformation("MediaFileRepository: StationIds in database: {StationIds}", 
                string.Join(", ", stationIds));
        }
        
        var result = await _db.MediaFiles
            .AsNoTracking()
            .Where(f => f.StationId == stationId)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);
        
        _logger.LogInformation("MediaFileRepository: Found {Count} media files for StationId={StationId}", 
            result.Count, stationId);
        
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
