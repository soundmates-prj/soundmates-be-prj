using LiveSessionService.Application.Features.Results;

namespace LiveSessionService.Application.Features.Common;

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

/// <summary>
/// AzuraCast API response data
/// Represents the actual API response structure
/// </summary>
public sealed class AzuraCastNowPlayingData
{
    public AzuraCastStationData? Station { get; init; }
    public AzuraCastCurrentSongData? NowPlaying { get; init; }
    public List<AzuraCastSongHistoryData>? SongHistory { get; init; }
}

public sealed class AzuraCastStationData
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public string? ShortCode { get; init; }
    public AzuraCastListenersData? Listeners { get; init; }
}

public sealed class AzuraCastCurrentSongData
{
    public long? ShId { get; init; } // Song history ID
    public AzuraCastSongData? Song { get; init; }
    public long? PlayedAt { get; init; } // Unix timestamp
    public long? Duration { get; init; }
    public int? Listeners { get; init; }
}

public sealed class AzuraCastSongData
{
    public string? Id { get; init; }
    public string? Text { get; init; } // Full text (Artist - Title)
    public string? Artist { get; init; }
    public string? Title { get; init; }
    public string? Album { get; init; }
    public string? Art { get; init; }
}

public sealed class AzuraCastSongHistoryData
{
    public long? ShId { get; init; }
    public long? PlayedAt { get; init; }
    public AzuraCastSongData? Song { get; init; }
}

public sealed class AzuraCastListenersData
{
    public int Current { get; init; }
    public int Unique { get; init; }
}
