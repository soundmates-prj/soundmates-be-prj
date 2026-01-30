namespace LiveSessionService.Application.Features.Common.AzuraCast.Models;

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
