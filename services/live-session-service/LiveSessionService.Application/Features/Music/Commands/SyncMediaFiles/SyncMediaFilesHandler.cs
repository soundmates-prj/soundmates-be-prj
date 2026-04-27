using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Constants;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Exceptions;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Music;
using LiveSessionService.Application.Services;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Music.Commands.SyncMediaFiles;

public sealed class SyncMediaFilesHandler : ICommandHandler<SyncMediaFilesCommand, SyncMediaFilesResult>
{
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly IStationMediaFileRepository _stationMediaFileRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IInputValidationService _validationService;
    private readonly ISyncConfigurationService _syncConfig;
    private readonly ILogger<SyncMediaFilesHandler> _logger;

    public SyncMediaFilesHandler(
        IAzuraCastStationRepository stationRepository,
        IMediaFileRepository mediaFileRepository,
        IStationMediaFileRepository stationMediaFileRepository,
        IAzuraCastClient azuraCastClient,
        IDateTimeProvider dateTimeProvider,
        IInputValidationService validationService,
        ISyncConfigurationService syncConfig,
        ILogger<SyncMediaFilesHandler> logger)
    {
        _stationRepository = stationRepository;
        _mediaFileRepository = mediaFileRepository;
        _stationMediaFileRepository = stationMediaFileRepository;
        _azuraCastClient = azuraCastClient;
        _dateTimeProvider = dateTimeProvider;
        _validationService = validationService;
        _syncConfig = syncConfig;
        _logger = logger;
    }

    public async Task<Result<SyncMediaFilesResult>> Handle(
        SyncMediaFilesCommand command,
        CancellationToken cancellationToken)
    {
        var startTime = _dateTimeProvider.UtcNow;
        
        try
        {
            _logger.LogInformation("Starting media file sync for station {StationId}", command.StationId);

            // Verify station exists
            var station = await _stationRepository.GetByIdAsync(command.StationId, cancellationToken);
            if (station == null)
            {
                _logger.LogWarning("Station {StationId} not found", command.StationId);
                return Result<SyncMediaFilesResult>.Failure("Station not found", ErrorCode.NotFound);
            }

            // Fetch media files from AzuraCast with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_syncConfig.SyncTimeout);

            var azuraFiles = await _azuraCastClient.GetStationFilesAsync(
                station.ExternalStationId, 
                cts.Token);

            // Validate data size
            if (azuraFiles.Count > _syncConfig.MaxMediaFilesPerSync)
            {
                _logger.LogWarning(
                    "Too many media files from AzuraCast: {Count}. Max allowed: {Max}",
                    azuraFiles.Count, _syncConfig.MaxMediaFilesPerSync);
                
                return Result<SyncMediaFilesResult>.Failure(
                    $"Too many media files ({azuraFiles.Count}). Maximum allowed: {_syncConfig.MaxMediaFilesPerSync}",
                    ErrorCode.BadRequest);
            }

            // Get existing station media mappings
            var existingMappings = await _stationMediaFileRepository.GetByStationIdAsync(
                command.StationId, 
                cancellationToken);
            
            var existingByAzuraId = existingMappings
                .ToDictionary(m => m.AzuraCastMediaId, StringComparer.OrdinalIgnoreCase);

            var newFiles = 0;
            var updatedFiles = 0;
            var unchangedFiles = 0;
            var errors = new List<string>();

            // Process in batches to prevent memory issues
            var batches = azuraFiles
                .Where(f => !string.IsNullOrWhiteSpace(f.UniqueId))
                .Chunk(_syncConfig.BatchSize);

            foreach (var batch in batches)
            {
                foreach (var azuraFile in batch)
                {
                    try
                    {
                        // Validate and sanitize input
                        var title = _validationService.SanitizeTitle(
                            azuraFile.Title ?? azuraFile.Text ?? Path.GetFileNameWithoutExtension(azuraFile.Path));
                        
                        var artist = _validationService.SanitizeArtist(azuraFile.Artist);
                        var album = _validationService.SanitizeAlbum(azuraFile.Album);
                        var genre = _validationService.SanitizeGenre(azuraFile.Genre);
                        var lyrics = _validationService.SanitizeString(azuraFile.Lyrics, 20000);
                        var normalizedLyrics = string.IsNullOrWhiteSpace(lyrics) ? null : lyrics;
                        var artUrl = _validationService.SanitizeUrl(azuraFile.Art);
                        var duration = _validationService.ValidateDuration(azuraFile.Length);
                        
                        var filePath = _validationService.SanitizeFilePath(azuraFile.Path);
                        var fileType = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
                        if (string.IsNullOrWhiteSpace(fileType))
                            fileType = "unknown";

                        // Check if mapping already exists
                        if (existingByAzuraId.TryGetValue(azuraFile.UniqueId!, out var existingMapping))
                        {
                            var mediaFile = existingMapping.MediaFile;
                            
                            // Check if update needed
                            var hasChanges = mediaFile.Title != title
                                             || mediaFile.Artist != artist
                                             || mediaFile.Album != album
                                             || mediaFile.Genre != genre
                                             || mediaFile.Lyrics != normalizedLyrics
                                             || mediaFile.ArtUrl != artUrl
                                             || mediaFile.DurationSeconds != duration
                                             || mediaFile.FileType != fileType;

                            if (!hasChanges)
                            {
                                unchangedFiles++;
                                continue;
                            }

                            // Update existing media file
                            mediaFile.Title = title;
                            mediaFile.Artist = artist;
                            mediaFile.Album = album;
                            mediaFile.Genre = genre;
                            mediaFile.Lyrics = normalizedLyrics;
                            mediaFile.ArtUrl = artUrl;
                            mediaFile.DurationSeconds = duration;
                            mediaFile.FileType = fileType;
                            mediaFile.UpdatedAt = _dateTimeProvider.UtcNow;

                            await _mediaFileRepository.UpdateAsync(mediaFile, cancellationToken);
                            updatedFiles++;
                            
                            _logger.LogDebug("Updated media file: {Title}", title);
                        }
                        else
                        {
                            // Create new media file
                            var uploadedAt = azuraFile.UploadedAt > 0
                                ? DateTimeOffset.FromUnixTimeSeconds(azuraFile.UploadedAt).UtcDateTime
                                : _dateTimeProvider.UtcNow;

                            var mediaFile = new MediaFile
                            {
                                Id = Guid.NewGuid(),
                                Title = title,
                                Artist = artist,
                                Album = album,
                                Genre = genre,
                                Lyrics = normalizedLyrics,
                                ArtUrl = artUrl,
                                DurationSeconds = duration,
                                FilePath = azuraFile.UniqueId!,
                                AzuraCastMediaId = azuraFile.UniqueId,
                                OriginalSourceType = "station",
                                FileType = fileType,
                                FileSizeBytes = 0,
                                UploadedByUserId = SystemUsers.AzuraCastSyncUser,
                                UploadedAt = uploadedAt
                            };

                            await _mediaFileRepository.AddAsync(mediaFile, cancellationToken);

                            // Create station media mapping
                            var stationMediaFile = new StationMediaFile
                            {
                                Id = Guid.NewGuid(),
                                MediaFileId = mediaFile.Id,
                                StationId = station.Id,
                                AzuraCastMediaId = azuraFile.UniqueId!,
                                ImportedAt = _dateTimeProvider.UtcNow
                            };

                            await _stationMediaFileRepository.AddAsync(stationMediaFile, cancellationToken);
                            newFiles++;
                            
                            _logger.LogDebug("Created new media file: {Title}", title);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Failed to sync media '{azuraFile.UniqueId}': {ex.Message}";
                        errors.Add(errorMsg);
                        _logger.LogWarning(ex, "Failed to sync media file {UniqueId}", azuraFile.UniqueId);
                    }
                }
            }

            var syncDuration = (int)(_dateTimeProvider.UtcNow - startTime).TotalSeconds;

            var result = new SyncMediaFilesResult
            {
                StationId = station.Id,
                StationName = station.StationName,
                Created = newFiles,
                Updated = updatedFiles,
                Skipped = unchangedFiles,
                Failed = errors.Count,
                Errors = errors,
            };

            _logger.LogInformation(
                "Media file sync completed for station {StationId}: " +
                "Created={Created}, Updated={Updated}, Unchanged={Unchanged}, Failed={Failed}, Duration={Duration}s",
                station.Id, newFiles, updatedFiles, unchangedFiles, errors.Count, syncDuration);

            return Result<SyncMediaFilesResult>.Success(result);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Media file sync timed out for station {StationId}", command.StationId);
            return Result<SyncMediaFilesResult>.Failure(
                "Sync operation timed out",
                ErrorCode.RequestTimeout);
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
