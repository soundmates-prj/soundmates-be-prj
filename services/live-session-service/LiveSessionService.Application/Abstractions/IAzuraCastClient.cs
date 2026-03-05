using LiveSessionService.Application.Features.Common.AzuraCast.Models;

namespace LiveSessionService.Application.Abstractions;

/// <summary>
/// Interface for AzuraCast API client
/// Defined in Application layer, implemented in Infrastructure
/// Application không quan tâm HTTP/JSON details
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

    /// <summary>Creates a playlist in AzuraCast for the given station</summary>
    Task<AzuraCastPlaylistData?> CreatePlaylistAsync(
        int stationId,
        string name,
        bool isAutoPlay,
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
}
