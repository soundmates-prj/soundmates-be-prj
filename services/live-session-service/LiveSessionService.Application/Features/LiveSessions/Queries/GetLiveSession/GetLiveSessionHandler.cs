using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Application.Features.LiveSessions.Scheduling;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSession;

public sealed class GetLiveSessionHandler : IQueryHandler<GetLiveSessionQuery, LiveSessionResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ISessionScheduleRepository _scheduleRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAzuraCastClient _azuraCastClient;
    private readonly IMediaFileRepository _mediaFileRepository;

    public GetLiveSessionHandler(
        ILiveSessionRepository sessionRepository,
        ISessionScheduleRepository scheduleRepository,
        IDateTimeProvider dateTimeProvider,
        IAzuraCastClient azuraCastClient,
        IMediaFileRepository mediaFileRepository)
    {
        _sessionRepository = sessionRepository;
        _scheduleRepository = scheduleRepository;
        _dateTimeProvider = dateTimeProvider;
        _azuraCastClient = azuraCastClient;
        _mediaFileRepository = mediaFileRepository;
    }

    public async Task<Result<LiveSessionResult>> Handle(
        GetLiveSessionQuery query,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdWithStationAsync(query.SessionId, cancellationToken);

        if (session == null)
        {
            return Result<LiveSessionResult>.Failure("Session not found", ErrorCode.NotFound);
        }

        DateTime? scheduleStartAt = null;
        if (session.Status == SessionStatus.Scheduled)
        {
            var schedules = await _scheduleRepository.GetByLiveSessionIdAsync(session.Id, cancellationToken);
            scheduleStartAt = ScheduleOccurrenceCalculator.GetNextOccurrenceUtc(schedules, _dateTimeProvider.UtcNow);
        }

        // Fetch now playing data from AzuraCast if session has a station
        NowPlayingTrackResult? currentTrack = null;
        NowPlayingTrackResult? playingNext = null;
        List<NowPlayingTrackResult> upcomingQueue = [];
        List<NowPlayingTrackResult> songHistory = [];

        AzuraCastNowPlayingData? nowPlayingData = null;
        if (session.AzuraCastStation != null)
        {
            nowPlayingData = await _azuraCastClient.GetNowPlayingAsync(
                session.AzuraCastStation.ExternalStationId, cancellationToken);

            if (nowPlayingData != null)
            {
                currentTrack = nowPlayingData.NowPlaying != null ? await MapTrackAsync(nowPlayingData.NowPlaying, cancellationToken) : null;
                playingNext = nowPlayingData.PlayingNext != null ? await MapTrackAsync(nowPlayingData.PlayingNext, cancellationToken) : null;
                songHistory = nowPlayingData.SongHistory?
                    .Select(MapHistoryTrack)
                    .ToList() ?? [];
                
                upcomingQueue = await MapQueueAsync(session.AzuraCastStation.ExternalStationId, cancellationToken);
            }
        }

        var nowPlaying = currentTrack != null ? new StationNowPlayingResult
        {
            ExternalStationId = session.AzuraCastStation?.ExternalStationId ?? 0,
            StationName = session.AzuraCastStation?.StationName ?? "",
            StationShortcode = session.AzuraCastStation?.StationShortcode,
            ListenUrl = session.AzuraCastStation?.StreamUrl,
            PublicPlayerUrl = session.AzuraCastStation?.PublicPlayerUrl,
            CurrentTrack = currentTrack,
            PlayingNext = playingNext,
            UpcomingQueue = upcomingQueue,
            SongHistory = songHistory,
            TotalListeners = nowPlayingData?.Listeners?.Unique ?? 0,
            UniqueListeners = nowPlayingData?.Listeners?.Unique ?? 0
        } : null;

        var result = new LiveSessionResult
        {
            
            Id = session.Id,
            UserId = session.HostUserId,
            StationId = session.AzuraCastStationId!.Value,
            StationName = session.AzuraCastStation?.StationName,
            SessionName = session.SessionName,
            Description = session.Description,
            Status = session.Status.ToString(),
            ScheduledStartAt = session.Status == SessionStatus.Scheduled
                ? scheduleStartAt ?? session.StartedAt
                : null,
            StartedAt = session.Status is SessionStatus.Live or SessionStatus.Paused or SessionStatus.Ended
                ? session.StartedAt
                : null,
            EndedAt = session.Status == SessionStatus.Ended
                ? session.EndedAt
                : null,
            TotalListeners = session.Listeners
                .Select(l => l.UserId.HasValue
                    ? $"u:{l.UserId.Value}"
                    : $"a:{l.AnonymousIdentifier ?? l.Id.ToString()}")
                .Distinct()
                .Count(),
            PeakListeners = session.Listeners.Count(l => l.IsConnected),
            ListenersCount = session.Listeners
                .Where(l => l.IsConnected)
                .Select(l => l.UserId.HasValue
                    ? $"u:{l.UserId.Value}"
                    : $"a:{l.AnonymousIdentifier ?? l.Id.ToString()}")
                .Distinct()
                .Count(),
            StreamUrl = session.AzuraCastStation?.StreamUrl,
            StationShortcode = session.AzuraCastStation?.StationShortcode,
            PublicPlayerUrl = session.AzuraCastStation?.PublicPlayerUrl,
            ThumbnailUrl = session.ThumbnailUrl,
            Genre = session.Genre,
            CreatedAt = session.CreatedAt,
            NowPlaying = nowPlaying
        };

        return Result<LiveSessionResult>.Success(result);
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
            ShId = track.ShId,
            Text = track.Song?.Text,
            Title = track.Song?.Title,
            Artist = track.Song?.Artist,
            Album = track.Song?.Album,
            Genre = track.Song?.Genre,
            ArtUrl = artUrl,
            Lyrics = lyrics,
            PlayedAt = track.PlayedAt,
            Duration = track.Duration,
            Elapsed = track.Elapsed,
            Remaining = track.Remaining,
            IsRequest = track.IsRequest
        };
    }

    private async Task<List<NowPlayingTrackResult>> MapQueueAsync(int externalStationId, CancellationToken cancellationToken)
    {
        try
        {
            var queueData = await _azuraCastClient.GetUpcomingQueueAsync(externalStationId, cancellationToken);
            if (queueData == null || queueData.Count == 0) return [];

            var mapped = new List<NowPlayingTrackResult>();
            foreach (var q in queueData)
            {
                mapped.Add(await MapTrackAsync(q, cancellationToken));
            }
            return mapped;
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static NowPlayingTrackResult MapHistoryTrack(AzuraCastSongHistoryData h)
        => new()
        {
            ShId = h.ShId,
            Text = h.Song?.Text,
            Title = h.Song?.Title,
            Artist = h.Song?.Artist,
            Album = h.Song?.Album,
            Genre = h.Song?.Genre,
            ArtUrl = h.Song?.Art,
            Lyrics = h.Song?.Lyrics,
            PlayedAt = h.PlayedAt
        };
}
