namespace LiveSessionService.Application.Features.Common.AzuraCast.Models;

public sealed class AzuraCastNowPlayingData
{
    public AzuraCastStationData? Station { get; init; }
    public AzuraCastListenersData? Listeners { get; init; }
    public AzuraCastCurrentSongData? NowPlaying { get; init; }
    public AzuraCastCurrentSongData? PlayingNext { get; init; }
    public List<AzuraCastSongHistoryData>? SongHistory { get; init; }
    public bool IsOnline { get; init; }
    public bool IsLive { get; init; }
    public string? StreamerName { get; init; }
}

