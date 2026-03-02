using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Infrastructure.Services.AzuraCast.ApiModels;

namespace LiveSessionService.Infrastructure.Services.AzuraCast.Mappers;

internal static class AzuraCastMapper
{
    // ?? Station list (/api/stations) ??????????????????????????????????????????

    public static AzuraCastStationListData ToApplicationModel(this AzuraCastApiStationListResponse api)
        => new()
        {
            Id = api.Id,
            Name = api.Name ?? "Unknown",
            Shortcode = api.ShortCode,
            Description = api.Description,
            ListenUrl = api.ListenUrl,
            PublicPlayerUrl = api.PublicPlayerUrl,
            IsPublic = api.IsPublic,
            HlsEnabled = api.HlsEnabled,
            HlsUrl = api.HlsUrl,
            Mounts = api.Mounts?.Select(m => m.ToApplicationModel()).ToList()
        };

    // ?? Now Playing (/api/nowplaying/{id}) ????????????????????????????????????

    public static AzuraCastNowPlayingData? ToApplicationModel(this AzuraCastApiResponse? api)
    {
        if (api == null) return null;
        return new()
        {
            Station   = api.Station?.ToApplicationModel(),
            Listeners = api.Listeners?.ToApplicationModel(),
            NowPlaying = api.NowPlaying?.ToApplicationModel(),
            PlayingNext = api.PlayingNext?.ToApplicationModel(),
            SongHistory = api.SongHistory?
                .Select(h => h.ToApplicationModel())
                .ToList(),
            IsOnline = api.IsOnline,
            IsLive = api.Live?.IsLive ?? false,
            StreamerName = string.IsNullOrWhiteSpace(api.Live?.StreamerName)
                ? null : api.Live.StreamerName
        };
    }

    // ?? Private helpers ???????????????????????????????????????????????????????

    private static AzuraCastStationData ToApplicationModel(this AzuraCastApiStation api)
        => new()
        {
            Id = api.Id,
            Name = api.Name ?? "Unknown",
            ShortCode = api.ShortCode,
            Description = api.Description,
            ListenUrl = api.ListenUrl,
            PublicPlayerUrl = api.PublicPlayerUrl,
            IsPublic = api.IsPublic,
            HlsEnabled = api.HlsEnabled,
            HlsUrl = api.HlsUrl,
            Mounts = api.Mounts?.Select(m => m.ToApplicationModel()).ToList()
        };

    private static AzuraCastCurrentSongData ToApplicationModel(this AzuraCastApiNowPlaying api)
        => new()
        {
            ShId      = api.ShId,
            Song      = api.Song?.ToApplicationModel(),
            PlayedAt  = (long)api.PlayedAt,
            Duration  = (long)api.Duration,
            Elapsed   = (long)api.Elapsed,
            Remaining = (long)api.Remaining,
            IsRequest = api.IsRequest
        };

    private static AzuraCastSongHistoryData ToApplicationModel(this AzuraCastApiSongHistory api)
        => new()
        {
            ShId = api.ShId,
            PlayedAt = api.PlayedAt,
            Song = api.Song?.ToApplicationModel()
        };

    private static AzuraCastSongData ToApplicationModel(this AzuraCastApiSong api)
        => new()
        {
            Id = api.Id,
            Text = api.Text,
            Title = api.Title,
            Artist = api.Artist,
            Album = api.Album,
            Genre = api.Genre,
            Art = api.Art,
            Lyrics = api.Lyrics
        };

    private static AzuraCastMountData ToApplicationModel(this AzuraCastApiMount api)
        => new()
        {
            Id = api.Id,
            Name = api.Name,
            Url = api.Url,
            Bitrate = api.Bitrate,
            Format = api.Format,
            Path = api.Path,
            IsDefault = api.IsDefault,
            Listeners = api.Listeners?.ToApplicationModel()
        };

    private static AzuraCastListenersData ToApplicationModel(this AzuraCastApiListeners api)
        => new()
        {
            Total = api.Total,
            Unique = api.Unique,
            Current = api.Current
        };
}
