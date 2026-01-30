using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Infrastructure.ExternalServices.AzuraCast.ApiModels;

namespace LiveSessionService.Infrastructure.ExternalServices.AzuraCast.Mappers;

/// <summary>
/// Maps internal API models to Application models
/// Tách mapping ra ?? AzuraCastClient g?n gàng
/// </summary>
internal static class AzuraCastMapper
{
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
