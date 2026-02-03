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
    
    /// <summary>
    /// Gets now playing information for a station
    /// BaseUrl ???c config trong Infrastructure qua HttpClient DI
    /// </summary>
    /// <param name="stationId">Station ID (integer: 1, 2, 3...)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Application model (not API model)</returns>
    Task<AzuraCastNowPlayingData?> GetNowPlayingAsync(
        int stationId,
        CancellationToken cancellationToken = default);
}
