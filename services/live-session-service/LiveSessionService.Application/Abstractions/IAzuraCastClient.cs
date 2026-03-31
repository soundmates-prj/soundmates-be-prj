using LiveSessionService.Application.Features.Common.AzuraCast.Models;

namespace LiveSessionService.Application.Abstractions;

/// <summary>
/// Interface for AzuraCast API client
/// Defined in Application layer, implemented in Infrastructure
/// Application doesn't care about HTTP/JSON details
/// </summary>
public interface IAzuraCastClient
{
    /// <summary>
    /// Gets list of all stations from AzuraCast
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of stations</returns>
    Task<List<AzuraCastStationListData>> GetStationsAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>Gets now playing information for a station</summary>
    Task<AzuraCastNowPlayingData?> GetNowPlayingAsync(
        int stationId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets all playlists for a station from AzuraCast</summary>
    Task<List<AzuraCastPlaylistData>> GetStationPlaylistsAsync(
        int stationId,
        CancellationToken cancellationToken = default);

    /// <summary>Gets all media files for a station from AzuraCast</summary>
    Task<List<AzuraCastStationFileData>> GetStationFilesAsync(
        int stationId,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a playlist in AzuraCast for the given station</summary>
    Task<AzuraCastPlaylistData?> CreatePlaylistAsync(
        int stationId,
        string name,
        bool isAutoPlay,
        bool includeInRequests,
        CancellationToken cancellationToken = default);

    /// <summary>Updates a playlist in AzuraCast for the given station</summary>
    Task<AzuraCastPlaylistData?> UpdatePlaylistAsync(
        int stationId,
        int playlistId,
        string name,
        bool isAutoPlay,
        bool includeInRequests,
        bool includeInOnDemand,
        bool isEnabled,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a playlist in AzuraCast for the given station</summary>
    Task DeletePlaylistAsync(
        int stationId,
        int playlistId,
        CancellationToken cancellationToken = default);

    /// <summary>Uploads an audio file to AzuraCast station media library</summary>
    Task<AzuraCastMediaData?> UploadMediaAsync(
        int stationId,
        Stream fileStream,
        string fileName,
        string contentType,
        string title,
        string artist,
        string? album,
        CancellationToken cancellationToken = default);

    /// <summary>Assigns an existing AzuraCast media file to a playlist</summary>
    Task AssignMediaToPlaylistAsync(
        int stationId,
        string fileUniqueId,
        int playlistId,
        CancellationToken cancellationToken = default);

    /// <summary>Removes an existing AzuraCast media file from a specific playlist</summary>
    Task RemoveMediaFromPlaylistAsync(
        int stationId,
        string fileUniqueId,
        int playlistId,
        CancellationToken cancellationToken = default);

    /// <summary>Queues a song request in AzuraCast for the given station</summary>
    Task QueueSongRequestAsync(
        int stationId,
        string mediaUniqueId,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes an existing AzuraCast media file from station library</summary>
    Task DeleteMediaAsync(
        int stationId,
        string fileUniqueId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the AzuraCast configuration at runtime.
    /// Called by AzuraCastConfigEventConsumer when admin changes the config.
    /// </summary>
    void UpdateConfig(string baseUrl, string apiKey);
}
