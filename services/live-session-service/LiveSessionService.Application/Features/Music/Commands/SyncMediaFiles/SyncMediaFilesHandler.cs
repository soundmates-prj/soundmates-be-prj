using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Exceptions;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Music;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Music.Commands.SyncMediaFiles;

public sealed class SyncMediaFilesHandler : ICommandHandler<SyncMediaFilesCommand, SyncMediaFilesResult>
{
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<SyncMediaFilesHandler> _logger;

    public SyncMediaFilesHandler(
        IAzuraCastStationRepository stationRepository,
        IMediaFileRepository mediaFileRepository,
        IAzuraCastClient azuraCastClient,
        IDateTimeProvider dateTimeProvider,
        ILogger<SyncMediaFilesHandler> logger)
    {
        _stationRepository = stationRepository;
        _mediaFileRepository = mediaFileRepository;
        _azuraCastClient = azuraCastClient;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<SyncMediaFilesResult>> Handle(
        SyncMediaFilesCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var station = await _stationRepository.GetByIdAsync(command.StationId, cancellationToken);
            if (station == null)
                return Result<SyncMediaFilesResult>.Failure("Station not found", ErrorCode.NotFound);

            var azuraFiles = await _azuraCastClient.GetStationFilesAsync(station.ExternalStationId, cancellationToken);
            var localFiles = await _mediaFileRepository.GetAllAsync(cancellationToken);

            var localFilesByUniqueId = localFiles
                .Where(f => !string.IsNullOrWhiteSpace(f.FilePath))
                .ToDictionary(f => f.FilePath, StringComparer.OrdinalIgnoreCase);

            var newFiles = 0;
            var updatedFiles = 0;
            var unchangedFiles = 0;

            foreach (var azuraFile in azuraFiles)
            {
                if (string.IsNullOrWhiteSpace(azuraFile.UniqueId))
                    continue;

                var title = azuraFile.Title
                            ?? azuraFile.Text
                            ?? Path.GetFileNameWithoutExtension(azuraFile.Path)
                            ?? "Unknown";

                var fileType = Path.GetExtension(azuraFile.Path).TrimStart('.').ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(fileType))
                    fileType = "unknown";

                if (localFilesByUniqueId.TryGetValue(azuraFile.UniqueId, out var existing))
                {
                    var hasChanges = existing.Title != title
                                     || existing.Artist != azuraFile.Artist
                                     || existing.Album != azuraFile.Album
                                     || existing.Genre != azuraFile.Genre
                                     || existing.ArtUrl != azuraFile.Art
                                     || existing.DurationSeconds != (int)azuraFile.Length
                                     || existing.FileType != fileType;

                    if (!hasChanges)
                    {
                        unchangedFiles++;
                        continue;
                    }

                    existing.Title = title;
                    existing.Artist = azuraFile.Artist;
                    existing.Album = azuraFile.Album;
                    existing.Genre = azuraFile.Genre;
                    existing.ArtUrl = azuraFile.Art;
                    existing.DurationSeconds = (int)azuraFile.Length;
                    existing.FileType = fileType;
                    existing.UpdatedAt = _dateTimeProvider.UtcNow;

                    await _mediaFileRepository.UpdateAsync(existing, cancellationToken);
                    updatedFiles++;
                    continue;
                }

                var uploadedAt = azuraFile.UploadedAt > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(azuraFile.UploadedAt).UtcDateTime
                    : _dateTimeProvider.UtcNow;

                var mediaFile = new MediaFile
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Artist = azuraFile.Artist,
                    Album = azuraFile.Album,
                    Genre = azuraFile.Genre,
                    ArtUrl = azuraFile.Art,
                    DurationSeconds = (int)azuraFile.Length,
                    FilePath = azuraFile.UniqueId,
                    FileType = fileType,
                    FileSizeBytes = 0,
                    UploadedByUserId = Guid.Empty,
                    UploadedAt = uploadedAt
                };

                await _mediaFileRepository.AddAsync(mediaFile, cancellationToken);
                newFiles++;
            }

            var result = new SyncMediaFilesResult
            {
                StationId = station.Id,
                StationName = station.StationName,
                TotalFilesInAzuraCast = azuraFiles.Count,
                NewFilesSynced = newFiles,
                UpdatedFiles = updatedFiles,
                UnchangedFiles = unchangedFiles
            };

            _logger.LogInformation(
                "Synced media files for station {StationId}: total={Total}, new={New}, updated={Updated}, unchanged={Unchanged}",
                station.Id,
                result.TotalFilesInAzuraCast,
                result.NewFilesSynced,
                result.UpdatedFiles,
                result.UnchangedFiles);

            return Result<SyncMediaFilesResult>.Success(result);
        }
        catch (AzuraCastException ex)
        {
            _logger.LogError(ex, "AzuraCast error while syncing media files for station {StationId}", command.StationId);
            return Result<SyncMediaFilesResult>.Failure(ex.Message, ex.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync media files for station {StationId}", command.StationId);
            return Result<SyncMediaFilesResult>.Failure(
                "Failed to sync media files from AzuraCast",
                ErrorCode.InternalServerError);
        }
    }
}
