using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Music;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Music.Commands.ImportSystemMediaBatch;

public sealed class ImportSystemMediaBatchHandler
    : ICommandHandler<ImportSystemMediaBatchCommand, ImportSystemMediaBatchResult>
{
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly IStationMediaFileRepository _stationMediaFileRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImportSystemMediaBatchHandler> _logger;

    public ImportSystemMediaBatchHandler(
        IAzuraCastStationRepository stationRepository,
        IMediaFileRepository mediaFileRepository,
        IStationMediaFileRepository stationMediaFileRepository,
        IAzuraCastClient azuraCastClient,
        IHttpClientFactory httpClientFactory,
        ILogger<ImportSystemMediaBatchHandler> logger)
    {
        _stationRepository = stationRepository;
        _mediaFileRepository = mediaFileRepository;
        _stationMediaFileRepository = stationMediaFileRepository;
        _azuraCastClient = azuraCastClient;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<ImportSystemMediaBatchResult>> Handle(
        ImportSystemMediaBatchCommand command,
        CancellationToken cancellationToken)
    {
        var requestedIds = command.MediaFileIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (requestedIds.Count == 0)
        {
            return Result<ImportSystemMediaBatchResult>.Failure(
                "Media file ids are required",
                ErrorCode.BadRequest);
        }

        var station = await _stationRepository.GetByIdAsync(command.StationId, cancellationToken);
        if (station is null)
        {
            return Result<ImportSystemMediaBatchResult>.Failure(
                "Station not found",
                ErrorCode.NotFound);
        }

        var mediaFiles = await _mediaFileRepository.GetByIdsAsync(requestedIds, cancellationToken);
        var mediaById = mediaFiles.ToDictionary(x => x.Id);

        var importedItems = new List<ImportedSystemMediaItemResult>();
        var errors = new List<string>();
        var skippedCount = 0;
        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(120);

        foreach (var mediaId in requestedIds)
        {
            if (!mediaById.TryGetValue(mediaId, out var media))
            {
                errors.Add($"Media '{mediaId}' not found");
                continue;
            }

            if (string.IsNullOrWhiteSpace(media.FilePath)
                || !media.FilePath.StartsWith("system://", StringComparison.OrdinalIgnoreCase))
            {
                skippedCount++;
                continue;
            }

            var existingMapping = await _stationMediaFileRepository.GetByMediaFileAndStationAsync(
                media.Id,
                station.Id,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(existingMapping?.AzuraCastMediaId))
            {
                skippedCount++;
                continue;
            }

            var sourceUrl = !string.IsNullOrWhiteSpace(media.FileUrl)
                ? media.FileUrl
                : (Uri.TryCreate(media.FilePath, UriKind.Absolute, out var absolute)
                    && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps)
                        ? media.FilePath
                        : null);

            if (string.IsNullOrWhiteSpace(sourceUrl))
            {
                errors.Add($"Media '{media.Title}' không có URL tải hợp lệ để import vào station");
                continue;
            }

            try
            {
                // Download audio from Cloudinary
                using var memoryStream = new MemoryStream();
                using var response = await httpClient.GetAsync(
                    sourceUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                response.EnsureSuccessStatusCode();
                await response.Content.CopyToAsync(memoryStream, cancellationToken);

                memoryStream.Position = 0;
                var extension = media.FileType.ToLowerInvariant();
                var tempFileName = $"{media.Title.Replace(" ", "_")}_{Guid.NewGuid():N}.{extension}";

                var uploaded = await _azuraCastClient.UploadMediaAsync(
                    station.ExternalStationId,
                    memoryStream,
                    tempFileName,
                    GetContentType(extension),
                    media.Title,
                    media.Artist ?? "Unknown Artist",
                    media.Album,
                    cancellationToken);

                if (uploaded is null || string.IsNullOrWhiteSpace(uploaded.UniqueId))
                {
                    errors.Add($"Failed to import '{media.Title}'");
                    continue;
                }

                // Create mapping record instead of updating MediaFile
                var stationMediaFile = new StationMediaFile
                {
                    Id = Guid.NewGuid(),
                    MediaFileId = media.Id,
                    StationId = station.Id,
                    AzuraCastMediaId = uploaded.UniqueId,
                    ImportedAt = DateTime.UtcNow
                };

                await _stationMediaFileRepository.AddAsync(stationMediaFile, cancellationToken);

                importedItems.Add(new ImportedSystemMediaItemResult
                {
                    MediaFileId = media.Id,
                    Title = media.Title,
                    StationMediaUniqueId = uploaded.UniqueId
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed importing system media {MediaId} into station {StationId}",
                    media.Id,
                    station.Id);
                errors.Add($"Failed to import '{media.Title}': {ex.Message}");
            }
        }

        var result = new ImportSystemMediaBatchResult
        {
            StationId = station.Id,
            StationName = station.StationName,
            RequestedCount = requestedIds.Count,
            ImportedCount = importedItems.Count,
            SkippedCount = skippedCount,
            FailedCount = errors.Count,
            ImportedItems = importedItems,
            Errors = errors
        };

        _logger.LogInformation(
            "Explicit system-media import finished for station {StationId}. Requested={Requested}, Imported={Imported}, Skipped={Skipped}, Failed={Failed}",
            station.Id,
            result.RequestedCount,
            result.ImportedCount,
            result.SkippedCount,
            result.FailedCount);

        return Result<ImportSystemMediaBatchResult>.Success(result);
    }

    private static string GetContentType(string fileType)
    {
        return fileType.ToLowerInvariant() switch
        {
            "mp3" => "audio/mpeg",
            "flac" => "audio/flac",
            "wav" => "audio/wav",
            "ogg" => "audio/ogg",
            _ => "application/octet-stream"
        };
    }
}
