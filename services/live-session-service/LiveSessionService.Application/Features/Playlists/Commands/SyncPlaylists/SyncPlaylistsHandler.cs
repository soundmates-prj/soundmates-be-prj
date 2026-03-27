using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Exceptions;
using LiveSessionService.Application.Exceptions;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Playlists.Commands.SyncPlaylists;

/// <summary>
/// Handler for syncing playlists from AzuraCast to local database
/// Also syncs media files associated with each playlist
/// </summary>
public sealed class SyncPlaylistsHandler : ICommandHandler<SyncPlaylistsCommand, SyncPlaylistsResult>
{
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IStationPlaylistRepository _playlistRepository;
    private readonly IPlaylistMediaRepository _playlistMediaRepository;
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly ILogger<SyncPlaylistsHandler> _logger;

    public SyncPlaylistsHandler(
        IAzuraCastStationRepository stationRepository,
        IStationPlaylistRepository playlistRepository,
        IPlaylistMediaRepository playlistMediaRepository,
        IMediaFileRepository mediaFileRepository,
        IAzuraCastClient azuraCastClient,
        ILogger<SyncPlaylistsHandler> logger)
    {
        _stationRepository = stationRepository;
        _playlistRepository = playlistRepository;
        _playlistMediaRepository = playlistMediaRepository;
        _mediaFileRepository = mediaFileRepository;
        _azuraCastClient = azuraCastClient;
        _logger = logger;
    }

    public async Task<Result<SyncPlaylistsResult>> Handle(
        SyncPlaylistsCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Syncing playlists from AzuraCast for station {StationId}", command.StationId);

            // Verify station exists
            var station = await _stationRepository.GetByIdAsync(command.StationId, cancellationToken);
            if (station == null)
            {
                _logger.LogWarning("Station {StationId} not found", command.StationId);
                return Result<SyncPlaylistsResult>.Failure(
                    "Station not found",
                    ErrorCode.NotFound);
            }

            // Get playlists from AzuraCast
            _logger.LogInformation(
                "Fetching playlists from AzuraCast for station {StationName} (External ID: {ExternalId})",
                station.StationName, station.ExternalStationId);

            var azuraPlaylists = await _azuraCastClient.GetStationPlaylistsAsync(
                station.ExternalStationId,
                cancellationToken);

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
                    Playlists = new List<PlaylistResult>()
                });
            }

            // Get all media files from AzuraCast for this station
            _logger.LogInformation("Fetching all media files from AzuraCast for station {ExternalId}", 
                station.ExternalStationId);
            var allMediaFiles = await _azuraCastClient.GetStationFilesAsync(
                station.ExternalStationId,
                cancellationToken);

            // Build a lookup dictionary: playlistId -> list of media files
            var playlistMediaMap = allMediaFiles
                .Where(f => f.Playlists != null && f.Playlists.Count > 0)
                .SelectMany(f => f.Playlists!.Select(p => new { File = f, PlaylistId = p.Id }))
                .GroupBy(x => x.PlaylistId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.File).ToList());

            _logger.LogInformation("Found {Count} media files across all playlists", allMediaFiles.Count);

            // Get existing playlists from database
            var existingPlaylists = await _playlistRepository.GetByStationIdAsync(
                station.Id,
                cancellationToken);

            var existingPlaylistMap = existingPlaylists
                .ToDictionary(p => p.ExternalPlaylistId, p => p);

            int newPlaylistCount = 0;
            int totalMediaSynced = 0;
            var allPlaylists = new List<PlaylistResult>();

            // Sync playlists and their media
            foreach (var azuraPlaylist in azuraPlaylists)
            {
                StationPlaylist playlist;

                if (existingPlaylistMap.TryGetValue(azuraPlaylist.Id, out var existingPlaylist))
                {
                    // Playlist already exists - update LastSyncedAt
                    existingPlaylist.LastSyncedAt = DateTime.UtcNow;
                    await _playlistRepository.UpdateAsync(existingPlaylist, cancellationToken);
                    playlist = existingPlaylist;
                    
                    _logger.LogDebug(
                        "Playlist {PlaylistName} (External ID: {ExternalId}) already exists",
                        azuraPlaylist.Name, azuraPlaylist.Id);
                }
                else
                {
                    // Create new playlist
                    playlist = new StationPlaylist
                    {
                        Id = Guid.NewGuid(),
                        AzuraCastStationId = station.Id,
                        ExternalPlaylistId = azuraPlaylist.Id,
                        PlaylistName = azuraPlaylist.Name,
                        Type = MapPlaylistType(azuraPlaylist.Type),
                        Source = MapPlaylistSource(azuraPlaylist.Source),
                        PlaylistOrder = MapPlaylistOrder(azuraPlaylist.Order),
                        IsEnabled = azuraPlaylist.IsEnabled,
                        IncludeInRequests = azuraPlaylist.IncludeInRequests,
                        IncludeInOnDemand = azuraPlaylist.IncludeInOnDemand,
                        Weight = azuraPlaylist.Weight,
                        CreatedAt = DateTime.UtcNow,
                        LastSyncedAt = DateTime.UtcNow
                    };

                    await _playlistRepository.AddAsync(playlist, cancellationToken);
                    newPlaylistCount++;

                    _logger.LogInformation(
                        "Synced new playlist {PlaylistName} (External ID: {ExternalId}) for station {StationName}",
                        playlist.PlaylistName, playlist.ExternalPlaylistId, station.StationName);
                }

                // Sync media files for this playlist
                var mediaSyncedCount = 0;
                if (playlistMediaMap.TryGetValue(azuraPlaylist.Id, out var mediaFilesInPlaylist))
                {
                    _logger.LogInformation(
                        "Syncing {Count} media files for playlist {PlaylistName}",
                        mediaFilesInPlaylist.Count, playlist.PlaylistName);

                    var playlistMediaList = new List<PlaylistMedia>();

                    foreach (var mediaFile in mediaFilesInPlaylist)
                    {
                        // Check if this media already exists in the playlist
                        var existingMedia = await _playlistMediaRepository.GetByPlaylistAndMediaIdAsync(
                            playlist.Id,
                            mediaFile.UniqueId,
                            cancellationToken);

                        if (existingMedia == null)
                        {
                            // Try to find the MediaFile in our database by FilePath (UniqueId)
                            var linkedMediaFile = await _mediaFileRepository.GetByFilePathAsync(
                                mediaFile.UniqueId,
                                cancellationToken);

                            var playlistMedia = new PlaylistMedia
                            {
                                Id = Guid.NewGuid(),
                                StationPlaylistId = playlist.Id,
                                MediaFileId = linkedMediaFile?.Id, // Link to MediaFile if exists
                                MediaId = mediaFile.UniqueId,
                                SongTitle = mediaFile.Title ?? mediaFile.Text ?? "Unknown",
                                SongArtist = mediaFile.Artist,
                                SongAlbum = mediaFile.Album,
                                SongArtUrl = mediaFile.Art,
                                DurationSeconds = (int)mediaFile.Length,
                                FilePath = mediaFile.Path,
                                PlayCount = 0,
                                Weight = 1,
                                IsEnabled = true,
                                CreatedAt = DateTime.UtcNow
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

                    if (playlistMediaList.Count > 0)
                    {
                        await _playlistMediaRepository.AddRangeAsync(playlistMediaList, cancellationToken);
                        _logger.LogInformation(
                            "Added {Count} new media files to playlist {PlaylistName}",
                            playlistMediaList.Count, playlist.PlaylistName);
                    }

                    totalMediaSynced += mediaSyncedCount;
                }

                // Get total track count for this playlist
                var playlistMedias = await _playlistMediaRepository.GetByPlaylistIdAsync(
                    playlist.Id,
                    cancellationToken);
                var totalDuration = playlistMedias.Sum(pm => pm.DurationSeconds);

                // Add to result list
                allPlaylists.Add(new PlaylistResult
                {
                    Id = playlist.Id,
                    StationId = playlist.AzuraCastStationId,
                    PlaylistName = playlist.PlaylistName,
                    Description = null,
                    IsAutoPlay = playlist.Type == Domain.Enums.PlaylistType.Default,
                    IncludeInRequests = playlist.IncludeInRequests,
                    TotalTracks = playlistMedias.Count,
                    TotalDuration = totalDuration,
                    CreatedAt = playlist.CreatedAt
                });
            }

            var result = new SyncPlaylistsResult
            {
                StationId = station.Id,
                StationName = station.StationName,
                TotalPlaylistsInAzuraCast = azuraPlaylists.Count,
                NewPlaylistsSynced = newPlaylistCount,
                ExistingPlaylists = existingPlaylists.Count,
                Playlists = allPlaylists,
                TotalMediaFilesSynced = totalMediaSynced
            };

            _logger.LogInformation(
                "Successfully synced playlists for station {StationName}: " +
                "{TotalPlaylists} playlists, {NewPlaylists} new, {MediaFiles} media files synced",
                station.StationName, azuraPlaylists.Count, newPlaylistCount, totalMediaSynced);

            return Result<SyncPlaylistsResult>.Success(result);
        }
        catch (AzuraCastException ex)
        {
            _logger.LogError(ex, "AzuraCast error while syncing playlists for station {StationId}", command.StationId);
            return Result<SyncPlaylistsResult>.Failure(
                ex.Message,
                ex.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync playlists for station {StationId}", command.StationId);
            return Result<SyncPlaylistsResult>.Failure(
                $"Failed to sync playlists: {ex.Message}",
                ErrorCode.InternalServerError);
        }
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
