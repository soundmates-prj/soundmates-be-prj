using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Music;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Music.Commands.ImportSystemMediaBatch;

public sealed class ImportSystemMediaBatchHandler
    : ICommandHandler<ImportSystemMediaBatchCommand, ImportSystemMediaBatchResult>
{
    private const string SystemMediaPrefix = "system://";

    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly ILogger<ImportSystemMediaBatchHandler> _logger;

    public ImportSystemMediaBatchHandler(
        IAzuraCastStationRepository stationRepository,
        IMediaFileRepository mediaFileRepository,
        IAzuraCastClient azuraCastClient,
        ILogger<ImportSystemMediaBatchHandler> logger)
    {
        _stationRepository = stationRepository;
        _mediaFileRepository = mediaFileRepository;
        _azuraCastClient = azuraCastClient;
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

        foreach (var mediaId in requestedIds)
        {
            if (!mediaById.TryGetValue(mediaId, out var media))
            {
                errors.Add($"Media '{mediaId}' not found");
                continue;
            }

            if (string.IsNullOrWhiteSpace(media.FilePath)
                || !media.FilePath.StartsWith(SystemMediaPrefix, StringComparison.OrdinalIgnoreCase))
            {
                skippedCount++;
                continue;
            }

            var relativePath = media.FilePath.Substring(SystemMediaPrefix.Length)
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            var absolutePath = Path.Combine(AppContext.BaseDirectory, "storage", relativePath);

            if (!File.Exists(absolutePath))
            {
                errors.Add($"System media '{media.Title}' not found on server");
                continue;
            }

            try
            {
                await using var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var uploaded = await _azuraCastClient.UploadMediaAsync(
                    station.ExternalStationId,
                    stream,
                    Path.GetFileName(absolutePath),
                    GetContentType(media.FileType),
                    media.Title,
                    media.Artist ?? "Unknown Artist",
                    media.Album,
                    cancellationToken);

                if (uploaded is null || string.IsNullOrWhiteSpace(uploaded.UniqueId))
                {
                    errors.Add($"Failed to import '{media.Title}'");
                    continue;
                }

                media.AzuraCastMediaId = uploaded.UniqueId;
                media.UpdatedAt = DateTime.UtcNow;
                await _mediaFileRepository.UpdateAsync(media, cancellationToken);

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
                errors.Add($"Failed to import '{media.Title}'");
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
