using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSessionStatistics;

public sealed class GetLiveSessionStatisticsHandler
    : IQueryHandler<GetLiveSessionStatisticsQuery, LiveSessionStatisticsResult>
{
    private readonly ILiveSessionRepository _liveSessionRepository;
    private readonly INowPlayingHistoryRepository _nowPlayingHistoryRepository;
    private readonly ISongRequestRepository _songRequestRepository;

    public GetLiveSessionStatisticsHandler(
        ILiveSessionRepository liveSessionRepository,
        INowPlayingHistoryRepository nowPlayingHistoryRepository,
        ISongRequestRepository songRequestRepository)
    {
        _liveSessionRepository = liveSessionRepository;
        _nowPlayingHistoryRepository = nowPlayingHistoryRepository;
        _songRequestRepository = songRequestRepository;
    }

    public async Task<Result<LiveSessionStatisticsResult>> Handle(
        GetLiveSessionStatisticsQuery query,
        CancellationToken cancellationToken)
    {
        var session = await _liveSessionRepository.GetByIdWithStationAsync(query.SessionId, cancellationToken);
        if (session == null)
        {
            return Result<LiveSessionStatisticsResult>.Failure("Session not found", ErrorCode.NotFound);
        }

        var songsPlayed = await _nowPlayingHistoryRepository.GetAllBySessionIdAsync(query.SessionId, cancellationToken);
        var songRequests = await _songRequestRepository.GetByLiveSessionIdAsync(query.SessionId, null, cancellationToken);

        var topRequestedSongs = songRequests
            .GroupBy(x => new
            {
                x.MediaFileId,
                x.MediaFile.Title,
                x.MediaFile.Artist
            })
            .Select(x => new SongRequestCountReportItemResult
            {
                MediaFileId = x.Key.MediaFileId,
                SongTitle = x.Key.Title,
                SongArtist = x.Key.Artist,
                RequestCount = x.Count()
            })
            .OrderByDescending(x => x.RequestCount)
            .ThenBy(x => x.SongTitle)
            .Take(5)
            .ToList();

        var hostName = session.Chats
            .Where(x => x.UserId == session.HostUserId && !string.IsNullOrWhiteSpace(x.UserName))
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.UserName)
            .FirstOrDefault();

        var report = new LiveSessionStatisticsResult
        {
            LiveSessionId = session.Id,
            SessionName = session.SessionName,
            Description = session.Description,
            StationId = session.AzuraCastStationId,
            StationName = session.AzuraCastStation?.StationName,
            HostUserId = session.HostUserId,
            HostName = hostName,
            Status = session.Status.ToString(),
            StartDate = session.StartedAt.HasValue ? DateOnly.FromDateTime(session.StartedAt.Value) : null,
            StartTime = session.StartedAt.HasValue ? TimeOnly.FromDateTime(session.StartedAt.Value) : null,
            EndDate = session.EndedAt.HasValue ? DateOnly.FromDateTime(session.EndedAt.Value) : null,
            EndTime = session.EndedAt.HasValue ? TimeOnly.FromDateTime(session.EndedAt.Value) : null,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            TotalDurationSeconds = CalculateTotalDurationSeconds(session.StartedAt, session.EndedAt),
            UniqueListeners = CountUniqueListeners(session.Listeners),
            PeakConcurrentListeners = CalculatePeakConcurrentListeners(session.Listeners, session.EndedAt),
            TotalMessages = session.Chats.Count,
            SongsPlayed = songsPlayed
                .OrderBy(x => x.PlayedAt)
                .Select(x => new PlayedSongReportItemResult
                {
                    Id = x.Id,
                    SongTitle = x.SongTitle,
                    SongArtist = x.SongArtist,
                    PlayedAt = x.PlayedAt,
                    EndedAt = x.EndedAt,
                    IsRequest = x.IsRequest
                })
                .ToList(),
            TotalSongRequests = songRequests.Count,
            TopRequestedSongs = topRequestedSongs,
            AcceptedRequests = songRequests.Count(x => x.Status == SongRequestStatus.Approved),
            RejectedRequests = songRequests.Count(x => x.Status == SongRequestStatus.Rejected)
        };

        return Result<LiveSessionStatisticsResult>.Success(report);
    }

    private static int CountUniqueListeners(IEnumerable<SessionListener> listeners)
    {
        return listeners
            .Select(ToListenerKey)
            .Distinct()
            .Count();
    }

    private static string ToListenerKey(SessionListener listener)
    {
        if (listener.UserId.HasValue)
        {
            return $"u:{listener.UserId.Value}";
        }

        if (!string.IsNullOrWhiteSpace(listener.AnonymousIdentifier))
        {
            return $"a:{listener.AnonymousIdentifier}";
        }

        return $"g:{listener.Id}";
    }

    private static int CalculatePeakConcurrentListeners(IEnumerable<SessionListener> listeners, DateTime? sessionEndedAt)
    {
        var fallbackEndTime = sessionEndedAt ?? DateTime.UtcNow;
        var events = new List<(DateTime Time, int Delta)>();

        foreach (var listener in listeners)
        {
            var disconnectedAt = listener.DisconnectedAt ?? fallbackEndTime;
            if (disconnectedAt < listener.ConnectedAt)
            {
                continue;
            }

            events.Add((listener.ConnectedAt, +1));
            events.Add((disconnectedAt, -1));
        }

        var orderedEvents = events
            .OrderBy(x => x.Time)
            .ThenByDescending(x => x.Delta)
            .ToList();

        var current = 0;
        var peak = 0;

        foreach (var e in orderedEvents)
        {
            current += e.Delta;
            if (current > peak)
            {
                peak = current;
            }
        }

        return peak;
    }

    private static int CalculateTotalDurationSeconds(DateTime? startedAt, DateTime? endedAt)
    {
        if (!startedAt.HasValue)
        {
            return 0;
        }

        var end = endedAt ?? DateTime.UtcNow;
        if (end <= startedAt.Value)
        {
            return 0;
        }

        return (int)(end - startedAt.Value).TotalSeconds;
    }
}
