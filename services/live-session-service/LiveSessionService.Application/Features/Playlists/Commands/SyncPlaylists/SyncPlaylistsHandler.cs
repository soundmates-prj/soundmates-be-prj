using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Constants;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Exceptions;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Application.Services;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Playlists.Commands.SyncPlaylists;

/// <summary>
/// Handler for syncing playlists from AzuraCast to local database with enhanced security
/// Also syncs media files associated with each playlist
/// </summary>
public sealed class SyncPlaylistsHandler : ICommandHandler<SyncPlaylistsCommand, SyncPlaylistsResult>
{
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IStationPlaylistRepository _playlistRepository;
    private readonly IPlaylistMediaRepository _playlistMediaRepository;
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IInputValidationService _validationService;
    private readonly ISyncConfigurationService _syncConfig;
    private readonly IAzuraCastErrorHandler _errorHandler;
    private readonly ILogger<SyncPlaylistsHandler> _logger;

    public SyncPlaylistsHandler(
        IAzuraCastStationRepository stationRepository,
        IStationPlaylistRepository playlistRepository,
        IPlaylistMediaRepository playlistMediaRepository,
        IMediaFileRepository mediaFileRepository,
        IAzuraCastClient azuraCastClient,
        IDateTimeProvider dateTimeProvider,
        IInputValidationService validationService,
        ISyncConfigurationService syncConfig,
        IAzuraCastErrorHandler errorHandler,
        ILogger<SyncPlaylistsHandler> logger)
    {
        _stationRepository = stationRepository;
        _playlistRepository = playlistRepository;
        _playlistMediaRepository = playlistMediaRepository;
        _mediaFileRepository = mediaFileRepository;
        _azuraCastClient = azuraCastClient;
        _dateTimeProvider = dateTimeProvider;
        _validationService = validationService;
        _syncConfig = syncConfig;
        _errorHandler = errorHandler;
        _logger = logger;
    }

    public async Task<Result<SyncPlaylistsResult>> Handle(
        SyncPlaylistsCommand command,
        CancellationToken cancellationToken)
    {
        var startTime = _dateTimeProvider.UtcNow;
        var attemptNumber = 0;
        Exception? lastException = null;

        while (attemptNumber < _syncConfig.MaxRetryAttempts)
        {
            attemptNumber++;
            
            try
            {
                _logger.LogInformation(
                    "Starting playlist sync for station {StationId} (Attempt {Attempt}/{Max})",
                    command.StationId, attemptNumber, _syncConfig.MaxRetryAttempts);

                // Verify station exists
                var station = await _stationRepository.GetByIdAsync(command.StationId, cancellationToken);
                if (station == null)
                {
                    _logger.LogWarning("Station {StationId} not found", command.StationId);
                    return Result<SyncPlaylistsResult>.Failure(
                        "Station not found",
                        ErrorCode.NotFound);
                }

                // Fetch playlists with timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_syncConfig.SyncTimeout);

                _logger.LogInformation(
                    "Fetching playlists from AzuraCast for station {StationName} (External ID: {ExternalId})",
                    station.StationName, station.ExternalStationId);

                var azuraPlaylists = await _azuraCastClient.GetStationPlaylistsAsync(
                    station.ExternalStationId,
                    cts.Token);

                // Validate data size
                if (azuraPlaylists.Count > _syncConfig.MaxPlaylistsPerSync)
                {
                    _logger.LogWarning(
                        "Too many playlists from AzuraCast: {Count}. Max allowed: {Max}",
                        azuraPlaylists.Count, _syncConfig.MaxPlaylistsPerSync);
                    
                    return Result<SyncPlaylistsResult>.Failure(
                        $"Too many playlists ({azuraPlaylists.Count}). Maximum allowed: {_syncConfig.MaxPlaylistsPerSync}",
                        ErrorCode.BadRequest);
                }

                if (azuraPlaylists.Count == 0)
                {
                    _logger.LogInformation("No playlists found in AzuraCast for station {StationId}", station.Id);
                    return Result<SyncPlaylistsResult>.Success(new SyncPlaylistsResult
                    {
                        StationId = station.Id,
                        StationName = station.StationName,
                        TotalPlaylistsInAzuraCast = 0,
                        NewPlaylistsSynced = 0,
                        ExistingPlaylists = 0,
                        DeletedPlaylists = 0,
                        Playlists = new List<PlaylistResult>()
                    });
                }

                // Get all media files from AzuraCast for this station
                _logger.LogInformation("Fetching all media files from AzuraCast for station {ExternalId}", 
                    station.ExternalStationId);
                var allMediaFiles = await _azuraCastClient.GetStationFilesAsync(
                    station.ExternalStationId,
                    cts.Token);

                // Build a lookup dictionary: playlistId -> list of media files
                var playlistMediaMap = allMediaFiles
                    .Where(f => f.Playlists != null && f.Playlists.Count > 0)
                    .SelectMany(f => f.Playlists!.Select(p => new { File = f, PlaylistId = p.Id }))
                    .GroupBy(x => x.PlaylistId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.File).ToList());

                _logger.LogInformation("Found {Count} media files across all playlists", allMediaFiles.Count);

                // Get existing playlists from database (including soft-deleted for restoration)
                var existingPlaylists = await _playlistRepository.GetAllByStationIdIncludingDeletedAsync(
                    station.Id,
                    cts.Token);

                var existingPlaylistMap = existingPlaylists
                    .ToDictionary(p => p.ExternalPlaylistId, p => p);

                int newPlaylistCount = 0;
                int updatedPlaylistCount = 0;
                int totalMediaSynced = 0;
                var allPlaylists = new List<PlaylistResult>();
                var errors = new List<string>();

                // Sync playlists and their media
                var azuraPlaylistIds = azuraPlaylists.Select(p => p.Id).ToHashSet();

                // Find playlists that exist in DB but were removed from AzuraCast
                var playlistsToDelete = existingPlaylists
                    .Where(p => !azuraPlaylistIds.Contains(p.ExternalPlaylistId) && !p.IsDeleted)
                    .ToList();

                // SOFT DELETE - Critical for data safety
                if (playlistsToDelete.Count > 0)
                {
                    var deletePercentage = (double)playlistsToDelete.Count / existingPlaylists.Count * 100;
                    
                    if (deletePercentage > 50)
                    {
                        _logger.LogWarning(
                            "SUSPICIOUS: Attempting to delete {Count}/{Total} playlists ({Percentage:F1}%). " +
                            "This might be an AzuraCast API error. Using soft delete for safety.",
                            playlistsToDelete.Count, existingPlaylists.Count, deletePercentage);
                    }

                    foreach (var playlistToDelete in playlistsToDelete)
                    {
                        // Soft delete instead of hard delete
                        playlistToDelete.IsDeleted = true;
                        playlistToDelete.DeletedAt = _dateTimeProvider.UtcNow;
                        playlistToDelete.DeletedBy = SystemUsers.AzuraCastSyncUser;
                        
                        await _playlistRepository.UpdateAsync(playlistToDelete, cts.Token);
                        
                        _logger.LogWarning(
                            "Soft deleted playlist {PlaylistName} (External ID: {ExternalId}). " +
                            "It was not found in AzuraCast. Can be restored if this was an error.",
                            playlistToDelete.PlaylistName, playlistToDelete.ExternalPlaylistId);
                    }
                }

                // Process playlists in batches
                var playlistBatches = azuraPlaylists.Chunk(_syncConfig.BatchSize);

                foreach (var batch in playlistBatches)
                {
                    foreach (var azuraPlaylist in batch)
                    {
                        try
                        {
                            // Validate and sanitize input
                            var playlistName = _validationService.SanitizeString(
                                azuraPlaylist.Name, 200, "Unknown Playlist");
                            var playlistDescription = string.IsNullOrWhiteSpace(azuraPlaylist.Description)
                                ? null
                                : _validationService.SanitizeString(azuraPlaylist.Description, 1000);

                            StationPlaylist playlist;

                            if (existingPlaylistMap.TryGetValue(azuraPlaylist.Id, out var existingPlaylist))
                            {
                                // Check if playlist was soft deleted - restore it
                                if (existingPlaylist.IsDeleted)
                                {
                                    existingPlaylist.IsDeleted = false;
                                    existingPlaylist.DeletedAt = null;
                                    existingPlaylist.DeletedBy = null;
                                    
                                    _logger.LogInformation(
                                        "Restored soft-deleted playlist {PlaylistName} (External ID: {ExternalId})",
                                        playlistName, azuraPlaylist.Id);
                                }

                                // Update playlist data
                                var hasChanges = false;

                                if (existingPlaylist.PlaylistName != playlistName)
                                {
                                    existingPlaylist.PlaylistName = playlistName;
                                    hasChanges = true;
                                }

                                if (existingPlaylist.Description != playlistDescription)
                                {
                                    existingPlaylist.Description = playlistDescription;
                                    hasChanges = true;
                                }

                                var newType = MapPlaylistType(azuraPlaylist.Type);
                                if (existingPlaylist.Type != newType)
                                {
                                    existingPlaylist.Type = newType;
                                    hasChanges = true;
                                }

                                var newSource = MapPlaylistSource(azuraPlaylist.Source);
                                if (existingPlaylist.Source != newSource)
                                {
                                    existingPlaylist.Source = newSource;
                                    hasChanges = true;
                                }

                                if (existingPlaylist.IsEnabled != azuraPlaylist.IsEnabled)
                                {
                                    existingPlaylist.IsEnabled = azuraPlaylist.IsEnabled;
                                    hasChanges = true;
                                }

                                if (existingPlaylist.Weight != azuraPlaylist.Weight)
                                {
                                    existingPlaylist.Weight = azuraPlaylist.Weight;
                                    hasChanges = true;
                                }

                                var newSongPlaybackOrder = MapSongPlaybackOrder(azuraPlaylist.Order);
                                if (existingPlaylist.SongPlaybackOrder != newSongPlaybackOrder)
                                {
                                    existingPlaylist.SongPlaybackOrder = newSongPlaybackOrder;
                                    hasChanges = true;
                                }

                                existingPlaylist.PlaylistOrder = MapPlaylistOrder(azuraPlaylist.Order);

                                existingPlaylist.LastSyncedAt = _dateTimeProvider.UtcNow;
                                await _playlistRepository.UpdateAsync(existingPlaylist, cts.Token);
                                playlist = existingPlaylist;

                                if (hasChanges)
                                {
                                    updatedPlaylistCount++;
                                    _logger.LogDebug(
                                        "Updated playlist {PlaylistName} (External ID: {ExternalId})",
                                        playlistName, azuraPlaylist.Id);
                                }
                            }
                            else
                            {
                                // Create new playlist
                                playlist = new StationPlaylist
                                {
                                    Id = Guid.NewGuid(),
                                    AzuraCastStationId = station.Id,
                                    ExternalPlaylistId = azuraPlaylist.Id,
                                    PlaylistName = playlistName,
                                    Description = playlistDescription,
                                    Type = MapPlaylistType(azuraPlaylist.Type),
                                    Source = MapPlaylistSource(azuraPlaylist.Source),
                                    SongPlaybackOrder = MapSongPlaybackOrder(azuraPlaylist.Order),
                                    PlaylistOrder = MapPlaylistOrder(azuraPlaylist.Order),
                                    IsEnabled = azuraPlaylist.IsEnabled,
                                    IncludeInRequests = azuraPlaylist.IncludeInRequests,
                                    IncludeInOnDemand = azuraPlaylist.IncludeInOnDemand,
                                    Weight = azuraPlaylist.Weight,
                                    IsDeleted = false,
                                    CreatedAt = _dateTimeProvider.UtcNow,
                                    LastSyncedAt = _dateTimeProvider.UtcNow
                                };

                                await _playlistRepository.AddAsync(playlist, cts.Token);
                                newPlaylistCount++;

                                _logger.LogInformation(
                                    "Synced new playlist {PlaylistName} (External ID: {ExternalId}) for station {StationName}",
                                    playlist.PlaylistName, playlist.ExternalPlaylistId, station.StationName);
                            }

                            // Sync media files for this playlist
                            var mediaSyncedCount = 0;
                            if (playlistMediaMap.TryGetValue(azuraPlaylist.Id, out var mediaFilesInPlaylist))
                            {
                                // Validate media count
                                if (mediaFilesInPlaylist.Count > _syncConfig.MaxMediaPerPlaylist)
                                {
                                    _logger.LogWarning(
                                        "Too many media files in playlist {PlaylistName}: {Count}. Max allowed: {Max}. Skipping media sync.",
                                        playlistName, mediaFilesInPlaylist.Count, _syncConfig.MaxMediaPerPlaylist);
                                    
                                    errors.Add($"Playlist '{playlistName}': Too many media files ({mediaFilesInPlaylist.Count})");
                                    continue;
                                }

                                _logger.LogInformation(
                                    "Syncing {Count} media files for playlist {PlaylistName}",
                                    mediaFilesInPlaylist.Count, playlist.PlaylistName);

                                var playlistMediaList = new List<PlaylistMedia>();

                                foreach (var mediaFile in mediaFilesInPlaylist)
                                {
                                    try
                                    {
                                        // Check if this media already exists in the playlist
                                        var existingMedia = await _playlistMediaRepository.GetByPlaylistAndMediaIdAsync(
                                            playlist.Id,
                                            mediaFile.UniqueId,
                                            cts.Token);

                                        if (existingMedia == null)
                                        {
                                            // Validate and sanitize media data
                                            var title = _validationService.SanitizeTitle(
                                                mediaFile.Title ?? mediaFile.Text ?? Path.GetFileNameWithoutExtension(mediaFile.Path));
                                            var artist = _validationService.SanitizeArtist(mediaFile.Artist);
                                            var album = _validationService.SanitizeAlbum(mediaFile.Album);
                                            var artUrl = _validationService.SanitizeUrl(mediaFile.Art);
                                            var duration = _validationService.ValidateDuration(mediaFile.Length);
                                            var filePath = _validationService.SanitizeFilePath(mediaFile.Path);

                                            // Try to find the MediaFile in our database by FilePath (UniqueId)
                                            var linkedMediaFile = await _mediaFileRepository.GetByFilePathAsync(
                                                mediaFile.UniqueId,
                                                cts.Token);

                                            var playlistMedia = new PlaylistMedia
                                            {
                                                Id = Guid.NewGuid(),
                                                StationPlaylistId = playlist.Id,
                                                MediaFileId = linkedMediaFile?.Id,
                                                MediaId = mediaFile.UniqueId,
                                                SongTitle = title,
                                                SongArtist = artist,
                                                SongAlbum = album,
                                                SongArtUrl = artUrl,
                                                DurationSeconds = duration,
                                                FilePath = filePath,
                                                PlayCount = 0,
                                                Weight = 1,
                                                IsEnabled = true,
                                                CreatedAt = _dateTimeProvider.UtcNow
                                            };

                                            playlistMediaList.Add(playlistMedia);
                                            mediaSyncedCount++;

                                            if (linkedMediaFile != null)
                                            {
                                                _logger.LogDebug(
                                                    "Linked media '{Title}' to MediaFile {MediaFileId}",
                                                    playlistMedia.SongTitle, linkedMediaFile.Id);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        var errorMsg = $"Failed to sync media '{mediaFile.UniqueId}' in playlist '{playlistName}': {ex.Message}";
                                        errors.Add(errorMsg);
                                        _logger.LogWarning(ex, "Failed to sync media file {UniqueId}", mediaFile.UniqueId);
                                    }
                                }

                                if (playlistMediaList.Count > 0)
                                {
                                    await _playlistMediaRepository.AddRangeAsync(playlistMediaList, cts.Token);
                                    _logger.LogInformation(
                                        "Added {Count} new media files to playlist {PlaylistName}",
                                        playlistMediaList.Count, playlist.PlaylistName);
                                }

                                totalMediaSynced += mediaSyncedCount;
                            }

                            // Get total track count for this playlist
                            var playlistMedias = await _playlistMediaRepository.GetByPlaylistIdAsync(
                                playlist.Id,
                                cts.Token);
                            var totalDuration = playlistMedias.Sum(pm => pm.DurationSeconds);

                            // Add to result list
                            allPlaylists.Add(new PlaylistResult
                            {
                                Id = playlist.Id,
                                StationId = playlist.AzuraCastStationId,
                                PlaylistName = playlist.PlaylistName,
                                Description = playlist.Description,
                                IsAutoPlay = playlist.Type == Domain.Enums.PlaylistType.Default,
                                IncludeInRequests = playlist.IncludeInRequests,
                                SongPlaybackOrder = playlist.SongPlaybackOrder.ToString(),
                                TotalTracks = playlistMedias.Count,
                                TotalDuration = totalDuration,
                                CreatedAt = playlist.CreatedAt
                            });
                        }
                        catch (Exception ex)
                        {
                            var errorMsg = $"Failed to sync playlist '{azuraPlaylist.Name}' (ID: {azuraPlaylist.Id}): {ex.Message}";
                            errors.Add(errorMsg);
                            _logger.LogError(ex, "Failed to process playlist {PlaylistId}", azuraPlaylist.Id);
                        }
                    }
                }

                var syncDuration = (int)(_dateTimeProvider.UtcNow - startTime).TotalSeconds;

                var result = new SyncPlaylistsResult
                {
                    StationId = station.Id,
                    StationName = station.StationName,
                    TotalPlaylistsInAzuraCast = azuraPlaylists.Count,
                    NewPlaylistsSynced = newPlaylistCount,
                    ExistingPlaylists = existingPlaylists.Count - playlistsToDelete.Count,
                    DeletedPlaylists = playlistsToDelete.Count,
                    Playlists = allPlaylists,
                    TotalMediaFilesSynced = totalMediaSynced
                };

                _logger.LogInformation(
                    "Successfully synced playlists for station {StationName}: " +
                    "{TotalPlaylists} playlists, {NewPlaylists} new, {UpdatedPlaylists} updated, " +
                    "{DeletedPlaylists} soft deleted, {MediaFiles} media files synced, {Errors} errors, Duration={Duration}s",
                    station.StationName, azuraPlaylists.Count, newPlaylistCount, updatedPlaylistCount,
                    playlistsToDelete.Count, totalMediaSynced, errors.Count, syncDuration);

                return Result<SyncPlaylistsResult>.Success(result);
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                // Check if we should retry
                if (_errorHandler.ShouldRetry(ex, attemptNumber))
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attemptNumber - 1) * 2); // Exponential backoff
                    _logger.LogWarning(
                        ex,
                        "Playlist sync attempt {Attempt} failed. Retrying in {Delay}s...",
                        attemptNumber, delay.TotalSeconds);
                    
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                // Don't retry, handle error
                break;
            }
        }

        // All retries failed, return error
        return HandleSyncError(lastException!, command.StationId);
    }

    private Result<SyncPlaylistsResult> HandleSyncError(Exception exception, Guid stationId)
    {
        var errorMessage = _errorHandler.GetUserFriendlyMessage(exception);
        var errorCode = ErrorCode.InternalServerError;

        if (exception is HttpRequestException httpEx)
        {
            errorCode = _errorHandler.MapHttpStatusToErrorCode(httpEx.StatusCode);
            _logger.LogError(httpEx, "HTTP error from AzuraCast while syncing playlists for station {StationId}", stationId);
        }
        else if (exception is OperationCanceledException)
        {
            errorCode = ErrorCode.RequestTimeout;
            _logger.LogWarning("Playlist sync operation timed out for station {StationId}", stationId);
        }
        else if (exception is AzuraCastException azEx)
        {
            errorCode = azEx.ErrorCode;
            _logger.LogError(azEx, "AzuraCast error while syncing playlists for station {StationId}", stationId);
        }
        else
        {
            _logger.LogError(exception, "Unexpected error during playlist sync for station {StationId}", stationId);
        }

        return Result<SyncPlaylistsResult>.Failure(errorMessage, errorCode);
    }

    private Domain.Enums.PlaylistType MapPlaylistType(string? type)
    {
        return type?.ToLowerInvariant() switch
        {
            "default" => Domain.Enums.PlaylistType.Default,
            "scheduled" => Domain.Enums.PlaylistType.Scheduled,
            "once_per_hour" => Domain.Enums.PlaylistType.OncePerHour,
            "once_per_day" => Domain.Enums.PlaylistType.OncePerDay,
            "advanced" => Domain.Enums.PlaylistType.Advanced,
            "jingle" => Domain.Enums.PlaylistType.Jingle,
            _ => Domain.Enums.PlaylistType.Default
        };
    }

    private Domain.Enums.PlaylistSource MapPlaylistSource(string? source)
    {
        return source?.ToLowerInvariant() switch
        {
            "songs" => Domain.Enums.PlaylistSource.Songs,
            "remote_url" => Domain.Enums.PlaylistSource.RemoteUrl,
            _ => Domain.Enums.PlaylistSource.Songs
        };
    }

    private Domain.Enums.SongPlaybackOrder MapSongPlaybackOrder(string? order)
    {
        return order?.ToLowerInvariant() switch
        {
            "shuffle" => Domain.Enums.SongPlaybackOrder.Shuffled,
            "random" => Domain.Enums.SongPlaybackOrder.Random,
            "sequential" => Domain.Enums.SongPlaybackOrder.Sequential,
            _ => Domain.Enums.SongPlaybackOrder.Sequential
        };
    }

    private int MapPlaylistOrder(string? order)
    {
        // AzuraCast returns order as string: "shuffle", "sequential", "random"
        // Map to int for storage (can be used for sorting or other purposes)
        return order?.ToLowerInvariant() switch
        {
            "shuffle" => 1,
            "sequential" => 2,
            "random" => 3,
            _ => 0
        };
    }
}
