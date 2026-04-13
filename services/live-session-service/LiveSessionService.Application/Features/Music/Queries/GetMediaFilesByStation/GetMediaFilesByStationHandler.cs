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
    private readonly IStationMediaFileRepository _stationMediaFiles;
    private readonly IAzuraCastStationRepository _stations;
    private readonly ILogger<GetMediaFilesByStationHandler> _logger;

    public GetMediaFilesByStationHandler(
        IStationMediaFileRepository stationMediaFiles,
        IAzuraCastStationRepository stations,
        ILogger<GetMediaFilesByStationHandler> logger)
    {
        _stationMediaFiles = stationMediaFiles;
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

            // Query StationMediaFile mapping table
            var stationMediaFiles = await _stationMediaFiles.GetByStationIdAsync(query.StationId, cancellationToken);

            _logger.LogInformation("GetMediaFilesByStation: Retrieved {Count} media files for StationId={StationId}",
                stationMediaFiles.Count, query.StationId);

            var results = stationMediaFiles
                .Select(smf => new MusicResult
                {
                    Id = smf.MediaFile.Id,
                    SourceType = "station",
                    Title = smf.MediaFile.Title,
                    Artist = smf.MediaFile.Artist ?? string.Empty,
                    Album = smf.MediaFile.Album,
                    ArtworkUrl = smf.MediaFile.ArtUrl,
                    Lyrics = smf.MediaFile.Lyrics,
                    Duration = smf.MediaFile.DurationSeconds,
                    FileUrl = !string.IsNullOrWhiteSpace(smf.MediaFile.FileUrl)
                        ? smf.MediaFile.FileUrl
                        : smf.MediaFile.FilePath,
                    FileType = smf.MediaFile.FileType,
                    FileSize = smf.MediaFile.FileSizeBytes,
                    UploadedAt = smf.MediaFile.UploadedAt,
                    AzuraCastMediaId = smf.AzuraCastMediaId,
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

