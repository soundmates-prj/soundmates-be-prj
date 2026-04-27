using LiveSessionService.Application.Features.Common.AzuraCast.Models;

namespace LiveSessionService.Application.Features.Common.AzuraCast;

/// <summary>
/// AzuraCast service interface
/// Defined in Application layer, implemented in Infrastructure
/// </summary>
public interface IAzuraCastService
{
    /// <summary>
    /// Gets now playing information from AzuraCast API
    /// </summary>
    /// <param name="baseUrl">AzuraCast base URL</param>
    /// <param name="stationId">Station ID (integer: 1, 2, 3...)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AzuraCast now playing data</returns>
    Task<AzuraCastNowPlayingData?> GetNowPlayingAsync(
        string baseUrl,
        int stationId,
        CancellationToken cancellationToken = default);
}
