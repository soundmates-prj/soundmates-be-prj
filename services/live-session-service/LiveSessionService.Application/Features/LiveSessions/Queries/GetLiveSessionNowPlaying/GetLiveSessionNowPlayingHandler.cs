using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSessionNowPlaying;

public sealed class GetLiveSessionNowPlayingHandler
    : IQueryHandler<GetLiveSessionNowPlayingQuery, StationNowPlayingResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly ILogger<GetLiveSessionNowPlayingHandler> _logger;

    public GetLiveSessionNowPlayingHandler(
        ILiveSessionRepository sessionRepository,
        IAzuraCastClient azuraCastClient,
        ILogger<GetLiveSessionNowPlayingHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _azuraCastClient = azuraCastClient;
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

        var result = new StationNowPlayingResult
        {
            ExternalStationId = session.AzuraCastStation.ExternalStationId,
            StationName       = data.Station?.Name ?? session.AzuraCastStation.StationName,
            StationShortcode  = data.Station?.ShortCode ?? session.AzuraCastStation.StationShortcode,
            ListenUrl         = data.Station?.ListenUrl ?? session.AzuraCastStation.StreamUrl,
            PublicPlayerUrl   = data.Station?.PublicPlayerUrl ?? session.AzuraCastStation.PublicPlayerUrl,
            IsOnline          = data.IsOnline,
            IsLive            = data.IsLive,
            StreamerName      = data.StreamerName,
            TotalListeners    = data.Listeners?.Total ?? 0,
            UniqueListeners   = data.Listeners?.Unique ?? 0,
            CurrentTrack      = data.NowPlaying != null ? MapTrack(data.NowPlaying) : null,
            PlayingNext       = data.PlayingNext != null ? MapTrack(data.PlayingNext) : null,
            SongHistory       = data.SongHistory?
                .Select(MapHistoryTrack)
                .ToList() ?? []
        };

        return Result<StationNowPlayingResult>.Success(result);
    }

    private static NowPlayingTrackResult MapTrack(AzuraCastCurrentSongData track)
        => new()
        {
            ShId      = track.ShId,
            Text      = track.Song?.Text,
            Title     = track.Song?.Title,
            Artist    = track.Song?.Artist,
            Album     = track.Song?.Album,
            Genre     = track.Song?.Genre,
            ArtUrl    = track.Song?.Art,
            Lyrics    = track.Song?.Lyrics,
            PlayedAt  = track.PlayedAt,
            Duration  = track.Duration,
            Elapsed   = track.Elapsed,
            Remaining = track.Remaining,
            IsRequest = track.IsRequest
        };

    private static NowPlayingTrackResult MapHistoryTrack(AzuraCastSongHistoryData track)
        => new()
        {
            ShId      = track.ShId,
            Text      = track.Song?.Text,
            Title     = track.Song?.Title,
            Artist    = track.Song?.Artist,
            Album     = track.Song?.Album,
            Genre     = track.Song?.Genre,
            ArtUrl    = track.Song?.Art,
            Lyrics    = track.Song?.Lyrics,
            PlayedAt  = track.PlayedAt,
            Duration  = track.Duration,
            IsRequest = track.IsRequest
        };
}
