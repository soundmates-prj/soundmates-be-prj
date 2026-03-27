using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Common.AzuraCast.Models;
using LiveSessionService.Application.Features.LiveSessions.Scheduling;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSession;

public sealed class GetLiveSessionHandler : IQueryHandler<GetLiveSessionQuery, LiveSessionResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ISessionScheduleRepository _scheduleRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAzuraCastClient _azuraCastClient;

    public GetLiveSessionHandler(
        ILiveSessionRepository sessionRepository,
        ISessionScheduleRepository scheduleRepository,
        IDateTimeProvider dateTimeProvider,
        IAzuraCastClient azuraCastClient)
    {
        _sessionRepository = sessionRepository;
        _scheduleRepository = scheduleRepository;
        _dateTimeProvider = dateTimeProvider;
        _azuraCastClient = azuraCastClient;
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
        List<NowPlayingTrackResult> songHistory = [];

        if (session.AzuraCastStation != null)
        {
            var nowPlayingData = await _azuraCastClient.GetNowPlayingAsync(
                session.AzuraCastStation.ExternalStationId, cancellationToken);

            if (nowPlayingData != null)
            {
                currentTrack = nowPlayingData.NowPlaying != null ? MapTrack(nowPlayingData.NowPlaying) : null;
                playingNext = nowPlayingData.PlayingNext != null ? MapTrack(nowPlayingData.PlayingNext) : null;
                songHistory = nowPlayingData.SongHistory?
                    .Select(MapHistoryTrack)
                    .ToList() ?? [];
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
            SongHistory = songHistory
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
            ListenersCount = session.Listeners.Count(l => l.IsConnected),
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

    private static NowPlayingTrackResult MapTrack(AzuraCastCurrentSongData track)
        => new()
        {
            ShId = track.ShId,
            Text = track.Song?.Text,
            Title = track.Song?.Title,
            Artist = track.Song?.Artist,
            Album = track.Song?.Album,
            Genre = track.Song?.Genre,
            ArtUrl = track.Song?.Art,
            Lyrics = track.Song?.Lyrics,
            PlayedAt = track.PlayedAt,
            Duration = track.Duration,
            Elapsed = track.Elapsed,
            Remaining = track.Remaining,
            IsRequest = track.IsRequest
        };

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
            PlayedAt = h.PlayedAt
        };
}
