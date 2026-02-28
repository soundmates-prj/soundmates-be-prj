using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Music;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Music.Queries.GetMediaFilesByStation;

public sealed class GetMediaFilesByStationHandler
    : IQueryHandler<GetMediaFilesByStationQuery, List<MusicResult>>
{
    private readonly IMediaFileRepository _mediaFiles;
    private readonly IAzuraCastStationRepository _stations;
    private readonly ILogger<GetMediaFilesByStationHandler> _logger;

    public GetMediaFilesByStationHandler(
        IMediaFileRepository mediaFiles,
        IAzuraCastStationRepository stations,
        ILogger<GetMediaFilesByStationHandler> logger)
    {
        _mediaFiles = mediaFiles;
        _stations = stations;
        _logger = logger;
    }

    public async Task<Result<List<MusicResult>>> Handle(
        GetMediaFilesByStationQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("GetMediaFilesByStation: Starting query for StationId={StationId}", query.StationId);

            // Ensure station exists for better error messages
            var station = await _stations.GetByIdAsync(query.StationId, cancellationToken);
            if (station is null)
            {
                _logger.LogWarning("GetMediaFilesByStation: Station not found - StationId={StationId}", query.StationId);
                return Result<List<MusicResult>>.Failure(
                    "Station not found",
                    ErrorCode.NotFound);
            }

            _logger.LogInformation("GetMediaFilesByStation: Station found - StationId={StationId}, StationName={StationName}", 
                station.Id, station.StationName);

            var entities = await _mediaFiles.GetByStationIdAsync(query.StationId, cancellationToken);

            _logger.LogInformation("GetMediaFilesByStation: Retrieved {Count} media files from database for StationId={StationId}", 
                entities.Count, query.StationId);

            var results = entities
                .Select(m => new MusicResult
                {
                    Id         = m.Id,
                    StationId  = station.Id,
                    Title      = m.Title,
                    Artist     = m.Artist ?? string.Empty,
                    Album      = m.Album,
                    ArtworkUrl = m.ArtUrl,
                    Duration   = m.DurationSeconds,
                    FileUrl    = m.FilePath,
                    FileType   = m.FileType,
                    FileSize   = m.FileSizeBytes,
                    UploadedAt = m.UploadedAt
                })
                .ToList();

            _logger.LogInformation(
                "GetMediaFilesByStation: Returning {Count} media files for station {StationId}",
                results.Count,
                station.Id);

            return Result<List<MusicResult>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMediaFilesByStation: Failed to get media files for station {StationId}", query.StationId);
            return Result<List<MusicResult>>.Failure(
                "Failed to retrieve media files for station",
                ErrorCode.InternalServerError);
        }
    }
}

