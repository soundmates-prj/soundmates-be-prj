using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSessionQueue;

public sealed class GetLiveSessionQueueHandler
    : IQueryHandler<GetLiveSessionQueueQuery, LiveSessionQueueResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IMediaFileRepository _mediaFileRepository;
    private readonly ILogger<GetLiveSessionQueueHandler> _logger;

    public GetLiveSessionQueueHandler(
        ILiveSessionRepository sessionRepository,
        IAzuraCastClient azuraCastClient,
        IMediaFileRepository mediaFileRepository,
        ILogger<GetLiveSessionQueueHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _azuraCastClient = azuraCastClient;
        _mediaFileRepository = mediaFileRepository;
        _logger = logger;
    }

    public async Task<Result<LiveSessionQueueResult>> Handle(
        GetLiveSessionQueueQuery query,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdWithStationAsync(query.SessionId, cancellationToken);
        if (session == null)
            return Result<LiveSessionQueueResult>.Failure("Session not found", ErrorCode.NotFound);

        if (session.AzuraCastStation == null)
            return Result<LiveSessionQueueResult>.Failure("Session does not have an associated station", ErrorCode.NotFound);

        _logger.LogInformation(
            "Fetching upcoming queue from AzuraCast for session {SessionId} (external station: {ExternalId})",
            query.SessionId, session.AzuraCastStation.ExternalStationId);

        var queueData = await _azuraCastClient.GetUpcomingQueueAsync(session.AzuraCastStation.ExternalStationId, cancellationToken);

        var mappedQueue = new List<NowPlayingTrackResult>();
        foreach (var track in queueData)
        {
            mappedQueue.Add(await MapTrackAsync(track, cancellationToken));
        }

        var result = new LiveSessionQueueResult
        {
            SessionId = query.SessionId,
            ExternalStationId = session.AzuraCastStation.ExternalStationId,
            Queue = mappedQueue
        };

        return Result<LiveSessionQueueResult>.Success(result);
    }

    private async Task<NowPlayingTrackResult> MapTrackAsync(AzuraCastCurrentSongData track, CancellationToken cancellationToken)
    {
        var lyrics = track.Song?.Lyrics;
        var artUrl = track.Song?.Art;
        
        if ((string.IsNullOrWhiteSpace(lyrics) || string.IsNullOrWhiteSpace(artUrl) || artUrl.Contains("generic_song")))
        {
            MediaFile? mediaFile = null;

            if (!string.IsNullOrWhiteSpace(track.Song?.Id))
            {
                mediaFile = await _mediaFileRepository.GetByAzuraCastMediaIdAsync(track.Song.Id, cancellationToken);
            }

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
                    lyrics = mediaFile.Lyrics;
                
                if ((string.IsNullOrWhiteSpace(artUrl) || artUrl.Contains("generic_song")) 
                    && !string.IsNullOrWhiteSpace(mediaFile.ArtUrl))
                    artUrl = mediaFile.ArtUrl;
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
