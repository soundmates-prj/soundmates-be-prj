using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSessionNowPlaying;

public sealed class GetLiveSessionNowPlayingHandler
    : IQueryHandler<GetLiveSessionNowPlayingQuery, StationNowPlayingResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly ILogger<GetLiveSessionNowPlayingHandler> _logger;

    public GetLiveSessionNowPlayingHandler(
        ILiveSessionRepository sessionRepository,
        IAzuraCastClient azuraCastClient,
        IMediaFileRepository mediaFileRepository,
        ILogger<GetLiveSessionNowPlayingHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _azuraCastClient = azuraCastClient;
        _mediaFileRepository = mediaFileRepository;
        _logger = logger;
    }

    public async Task<Result<StationNowPlayingResult>> Handle(
        GetLiveSessionNowPlayingQuery query,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdWithStationAsync(query.SessionId, cancellationToken);
        if (session == null)
            return Result<StationNowPlayingResult>.Failure("Session not found", ErrorCode.NotFound);

        if (session.AzuraCastStation == null)
            return Result<StationNowPlayingResult>.Failure("Session does not have an associated station", ErrorCode.NotFound);

        _logger.LogInformation(
            "Fetching live now playing from AzuraCast for session {SessionId} (external station: {ExternalId})",
            query.SessionId, session.AzuraCastStation.ExternalStationId);

        var data = await _azuraCastClient.GetNowPlayingAsync(session.AzuraCastStation.ExternalStationId, cancellationToken);

        if (data == null)
            return Result<StationNowPlayingResult>.Failure(
                "No now playing data returned from AzuraCast", ErrorCode.NotFound);

        var listenUrl = ResolvePublicUrl(data.Station?.ListenUrl, session.AzuraCastStation.StreamUrl);
        var publicPlayerUrl = ResolvePublicUrl(data.Station?.PublicPlayerUrl, session.AzuraCastStation.PublicPlayerUrl);

        var result = new StationNowPlayingResult
        {
            ExternalStationId = session.AzuraCastStation.ExternalStationId,
            StationName       = data.Station?.Name ?? session.AzuraCastStation.StationName,
            StationShortcode  = data.Station?.ShortCode ?? session.AzuraCastStation.StationShortcode,
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

    private static NowPlayingTrackResult MapHistoryTrack(AzuraCastSongHistoryData track)
        => new()
        {
            ShId      = track.ShId,
            Text      = track.Song?.Text,
            Title     = track.Song?.Title,
            Artist    = track.Song?.Artist,
            Album     = track.Song?.Album,
            Genre     = track.Song?.Genre,
            ArtUrl    = ResolveHttpsUrl(track.Song?.Art),
            Lyrics    = track.Song?.Lyrics,
            PlayedAt  = track.PlayedAt,
            Duration  = track.Duration,
            IsRequest = track.IsRequest
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
