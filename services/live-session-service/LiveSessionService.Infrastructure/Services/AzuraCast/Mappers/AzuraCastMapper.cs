using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Infrastructure.Services.AzuraCast.ApiModels;

namespace LiveSessionService.Infrastructure.Services.AzuraCast.Mappers;

/// <summary>
/// Maps internal API models to Application models
/// Tách mapping ra ?? AzuraCastClient g?n gàng
/// </summary>
internal static class AzuraCastMapper
{
    /// <summary>
    /// Convert API station list response to Application model
    /// </summary>
    public static AzuraCastStationListData ToApplicationModel(this AzuraCastApiStationListResponse apiStation)
    {
        return new AzuraCastStationListData
        {
            Id = apiStation.Id,
            Name = apiStation.Name ?? "Unknown",
            Shortcode = apiStation.ShortCode,
            Description = apiStation.Description,
            ListenUrl = apiStation.ListenUrl,
            PublicPlayerUrl = apiStation.PublicPlayerUrl,
            IsPublic = apiStation.IsPublic,
            Mounts = apiStation.Mounts?
                .Select(m => m.ToApplicationModel())
                .ToList(),
            HlsEnabled = apiStation.HlsEnabled,
            HlsUrl = apiStation.HlsUrl
        };
    }

    private static AzuraCastMountData ToApplicationModel(this AzuraCastApiMount apiMount)
    {
        return new AzuraCastMountData
        {
            Id = apiMount.Id,
            Name = apiMount.Name,
            Url = apiMount.Url,
            Bitrate = apiMount.Bitrate,
            Format = apiMount.Format,
            Listeners = apiMount.Listeners != null ? new AzuraCastListenersData
            {
                Current = apiMount.Listeners.Current,
                Unique = apiMount.Listeners.Unique
            } : null,
            Path = apiMount.Path,
            IsDefault = apiMount.IsDefault
        };
    }
    
    /// <summary>
    /// Convert API response to Application model
    /// </summary>
    public static AzuraCastNowPlayingData? ToApplicationModel(this AzuraCastApiResponse? apiResponse)
    {
        if (apiResponse == null)
            return null;

        return new AzuraCastNowPlayingData
        {
            Station = apiResponse.Station?.ToApplicationModel(),
            NowPlaying = apiResponse.NowPlaying?.ToApplicationModel(),
            SongHistory = apiResponse.SongHistory?
                .Select(h => h.ToApplicationModel())
                .Where(h => h != null)
                .Cast<AzuraCastSongHistoryData>()
                .ToList()
        };
    }

    private static AzuraCastStationData? ToApplicationModel(this AzuraCastApiStation? apiStation)
    {
        if (apiStation == null)
            return null;

        return new AzuraCastStationData
        {
            Id = apiStation.Id,
            Name = apiStation.Name ?? "Unknown",
            ShortCode = apiStation.ShortCode,
            Listeners = apiStation.Listeners?.ToApplicationModel()
        };
    }

    private static AzuraCastCurrentSongData? ToApplicationModel(this AzuraCastApiNowPlaying? apiNowPlaying)
    {
        if (apiNowPlaying == null)
            return null;

        return new AzuraCastCurrentSongData
        {
            ShId = apiNowPlaying.ShId,
            Song = apiNowPlaying.Song?.ToApplicationModel(),
            PlayedAt = apiNowPlaying.PlayedAt,
            Duration = apiNowPlaying.Duration,
            Listeners = apiNowPlaying.Listeners
        };
    }

    private static AzuraCastSongData? ToApplicationModel(this AzuraCastApiSong? apiSong)
    {
        if (apiSong == null)
            return null;

        return new AzuraCastSongData
        {
            Id = apiSong.Id,
            Text = apiSong.Text,
            Artist = apiSong.Artist,
            Title = apiSong.Title,
            Album = apiSong.Album,
            Art = apiSong.Art
        };
    }

    private static AzuraCastSongHistoryData? ToApplicationModel(this AzuraCastApiSongHistory? apiHistory)
    {
        if (apiHistory == null)
            return null;

        return new AzuraCastSongHistoryData
        {
            ShId = apiHistory.ShId,
            PlayedAt = apiHistory.PlayedAt,
            Song = apiHistory.Song?.ToApplicationModel()
        };
    }

    private static AzuraCastListenersData? ToApplicationModel(this AzuraCastApiListeners? apiListeners)
    {
        if (apiListeners == null)
            return null;

        return new AzuraCastListenersData
        {
            Current = apiListeners.Current,
            Unique = apiListeners.Unique
        };
    }
}
