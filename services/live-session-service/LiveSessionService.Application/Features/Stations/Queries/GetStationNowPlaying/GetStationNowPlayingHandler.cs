using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Exceptions;
using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.Stations.Queries.GetStationNowPlaying;

public sealed class GetStationNowPlayingHandler
    : IQueryHandler<GetStationNowPlayingQuery, StationNowPlayingResult>
{
    private readonly IAzuraCastStationRepository _stationRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly ILogger<GetStationNowPlayingHandler> _logger;

    public GetStationNowPlayingHandler(
        IAzuraCastStationRepository stationRepository,
        IAzuraCastClient azuraCastClient,
        ILogger<GetStationNowPlayingHandler> logger)
    {
        _stationRepository = stationRepository;
        _azuraCastClient = azuraCastClient;
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

        var result = new StationNowPlayingResult
        {
            ExternalStationId = station.ExternalStationId,
            StationName       = data.Station?.Name ?? station.StationName,
            StationShortcode  = data.Station?.ShortCode ?? station.StationShortcode,
            ListenUrl         = data.Station?.ListenUrl ?? station.StreamUrl,
            PublicPlayerUrl   = data.Station?.PublicPlayerUrl ?? station.PublicPlayerUrl,
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

    private static NowPlayingTrackResult MapHistoryTrack(AzuraCastSongHistoryData h)
        => new()
        {
            ShId     = h.ShId,
            Text     = h.Song?.Text,
            Title    = h.Song?.Title,
            Artist   = h.Song?.Artist,
            Album    = h.Song?.Album,
            Genre    = h.Song?.Genre,
            ArtUrl   = h.Song?.Art,
            PlayedAt = h.PlayedAt
        };
}
