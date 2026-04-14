using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Exceptions;
using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Stations.Queries.GetStationNowPlaying;

public sealed class GetStationNowPlayingHandler
    : IQueryHandler<GetStationNowPlayingQuery, StationNowPlayingResult>
{
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly ILogger<GetStationNowPlayingHandler> _logger;

    public GetStationNowPlayingHandler(
        IAzuraCastStationRepository stationRepository,
        IAzuraCastClient azuraCastClient,
        IMediaFileRepository mediaFileRepository,
        ILogger<GetStationNowPlayingHandler> logger)
    {
        _stationRepository = stationRepository;
        _azuraCastClient = azuraCastClient;
        _mediaFileRepository = mediaFileRepository;
        _logger = logger;
    }

    public async Task<Result<StationNowPlayingResult>> Handle(
        GetStationNowPlayingQuery query,
        CancellationToken cancellationToken)
    {
        var station = await _stationRepository.GetByIdAsync(query.StationId, cancellationToken);
        if (station == null)
            return Result<StationNowPlayingResult>.Failure("Station not found", ErrorCode.NotFound);

        _logger.LogInformation(
            "Fetching live now playing from AzuraCast for station {StationId} (external: {ExternalId})",
            query.StationId, station.ExternalStationId);

        AzuraCastNowPlayingData? data;
        try
        {
            data = await _azuraCastClient.GetNowPlayingAsync(station.ExternalStationId, cancellationToken);
        }
        catch (AzuraCastException ex)
        {
            _logger.LogWarning(ex,
                "AzuraCast now-playing request failed for station {StationId} (external: {ExternalId})",
                station.Id, station.ExternalStationId);
            return Result<StationNowPlayingResult>.Failure(ex.Message, ex.ErrorCode);
        }

        if (data == null)
            return Result<StationNowPlayingResult>.Failure(
                "No now playing data returned from AzuraCast", ErrorCode.NotFound);

        var listenUrl = ResolvePublicUrl(data.Station?.ListenUrl, station.StreamUrl);
        var publicPlayerUrl = ResolvePublicUrl(data.Station?.PublicPlayerUrl, station.PublicPlayerUrl);

        var result = new StationNowPlayingResult
        {
            ExternalStationId = station.ExternalStationId,
            StationName       = data.Station?.Name ?? station.StationName,
            StationShortcode  = data.Station?.ShortCode ?? station.StationShortcode,
            ListenUrl         = listenUrl,
            PublicPlayerUrl   = publicPlayerUrl,
            IsOnline          = data.IsOnline,
            IsLive            = data.IsLive,
            StreamerName      = data.StreamerName,
            TotalListeners    = data.Listeners?.Total ?? 0,
            UniqueListeners   = data.Listeners?.Unique ?? 0,
            CurrentTrack      = data.NowPlaying != null ? await MapTrackAsync(data.NowPlaying, cancellationToken) : null,
            PlayingNext       = data.PlayingNext != null ? await MapTrackAsync(data.PlayingNext, cancellationToken) : null,
            SongHistory       = data.SongHistory?
                .Select(MapHistoryTrack)
                .ToList() ?? []
        };

        return Result<StationNowPlayingResult>.Success(result);
    }

    private async Task<NowPlayingTrackResult> MapTrackAsync(AzuraCastCurrentSongData track, CancellationToken cancellationToken)
    {
        var lyrics = track.Song?.Lyrics;
        var artUrl = track.Song?.Art;
        
        // If AzuraCast doesn't have lyrics, try to get them from our DB
        // Also fallback for missing artwork
        if ((string.IsNullOrWhiteSpace(lyrics) || string.IsNullOrWhiteSpace(artUrl) || artUrl.Contains("generic_song")))
        {
            MediaFile? mediaFile = null;

            // 1. Try to match by SongId (which usually matches our UniqueId / AzuraCastMediaId)
            if (!string.IsNullOrWhiteSpace(track.Song?.Id))
            {
                mediaFile = await _mediaFileRepository.GetByAzuraCastMediaIdAsync(track.Song.Id, cancellationToken);
            }

            // 2. Try to match by title and artist if SongId fails or is missing
            if (mediaFile == null && !string.IsNullOrWhiteSpace(track.Song?.Title))
            {
                var files = await _mediaFileRepository.GetAllAsync(cancellationToken);
                mediaFile = files.FirstOrDefault(f => 
                    string.Equals(f.Title, track.Song.Title, StringComparison.OrdinalIgnoreCase) && 
                    (string.IsNullOrWhiteSpace(track.Song.Artist) || string.Equals(f.Artist, track.Song.Artist, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (mediaFile != null)
            {
                if (string.IsNullOrWhiteSpace(lyrics) && !string.IsNullOrWhiteSpace(mediaFile.Lyrics))
                {
                    lyrics = mediaFile.Lyrics;
                }
                
                if ((string.IsNullOrWhiteSpace(artUrl) || artUrl.Contains("generic_song")) 
                    && !string.IsNullOrWhiteSpace(mediaFile.ArtUrl))
                {
                    artUrl = mediaFile.ArtUrl;
                }
            }
        }

        return new NowPlayingTrackResult
        {
            ShId      = track.ShId,
            Text      = track.Song?.Text,
            Title     = track.Song?.Title,
            Artist    = track.Song?.Artist,
            Album     = track.Song?.Album,
            Genre     = track.Song?.Genre,
            ArtUrl    = ResolveHttpsUrl(artUrl),
            Lyrics    = lyrics,
            PlayedAt  = track.PlayedAt,
            Duration  = track.Duration,
            Elapsed   = track.Elapsed,
            Remaining = track.Remaining,
            IsRequest = track.IsRequest
        };
    }

    private static NowPlayingTrackResult MapHistoryTrack(AzuraCastSongHistoryData h)
        => new()
        {
            ShId     = h.ShId,
            Text     = h.Song?.Text,
            Title    = h.Song?.Title,
            Artist   = h.Song?.Artist,
            Album    = h.Song?.Album,
            Genre    = h.Song?.Genre,
            ArtUrl   = ResolveHttpsUrl(h.Song?.Art),
            Lyrics   = h.Song?.Lyrics,
            PlayedAt = h.PlayedAt
        };

    private static string? ResolvePublicUrl(string? candidateUrl, string? fallbackUrl)
    {
        if (LooksLikeInternalUrl(candidateUrl) && !string.IsNullOrWhiteSpace(fallbackUrl))
            return fallbackUrl;

        return string.IsNullOrWhiteSpace(candidateUrl) ? fallbackUrl : candidateUrl;
    }

    private static bool LooksLikeInternalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        return uri.IsLoopback
            || uri.Host.Equals("host.docker.internal", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveHttpsUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + url.Substring(7);
        }
        return url;
    }
}
