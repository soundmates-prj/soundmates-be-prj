using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.LiveSessions.Scheduling;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetAllLiveSessions;

public sealed class GetAllLiveSessionsHandler : IQueryHandler<GetAllLiveSessionsQuery, PagedResult<LiveSessionResult>>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ISessionScheduleRepository _scheduleRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAllLiveSessionsHandler(
        ILiveSessionRepository sessionRepository,
        ISessionScheduleRepository scheduleRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _sessionRepository = sessionRepository;
        _scheduleRepository = scheduleRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PagedResult<LiveSessionResult>>> Handle(
        GetAllLiveSessionsQuery query,
        CancellationToken cancellationToken)
    {
        var sessions = await _sessionRepository.GetAllWithStationsAsync(cancellationToken);

        if (query.UserId.HasValue)
        {
            sessions = sessions.Where(s => s.HostUserId == query.UserId.Value).ToList();
        }

        if (!string.IsNullOrEmpty(query.Status))
        {
            sessions = sessions.Where(s => s.Status.ToString().Equals(query.Status, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var scheduledIds = sessions
            .Where(x => x.Status == SessionStatus.Scheduled)
            .Select(x => x.Id)
            .ToList();

        var schedulesBySession = await _scheduleRepository.GetByLiveSessionIdsAsync(scheduledIds, cancellationToken);

        var results = sessions.Select(s =>
        {
            schedulesBySession.TryGetValue(s.Id, out var schedules);

            var scheduleStartAt = schedules is null
                ? null
                : ScheduleOccurrenceCalculator.GetNextOccurrenceUtc(schedules, _dateTimeProvider.UtcNow);

            return new LiveSessionResult
            {
                Id = s.Id,
                UserId = s.HostUserId,
                StationId = s.AzuraCastStationId!.Value,
                StationName = s.AzuraCastStation?.StationName,
                SessionName = s.SessionName,
                Description = s.Description,
                Status = s.Status.ToString(),
                ScheduledStartAt = s.Status == SessionStatus.Scheduled
                    ? scheduleStartAt ?? s.StartedAt
                    : null,
                StartedAt = s.Status is SessionStatus.Live or SessionStatus.Paused or SessionStatus.Ended
                    ? s.StartedAt
                    : null,
                EndedAt = s.Status == SessionStatus.Ended
                    ? s.EndedAt
                    : null,
                TotalListeners = s.Listeners
                    .Select(l => l.UserId.HasValue
                        ? $"u:{l.UserId.Value}"
                        : $"a:{l.AnonymousIdentifier ?? l.Id.ToString()}")
                    .Distinct()
                    .Count(),
                PeakListeners = s.Listeners.Count(l => l.IsConnected),
                ListenersCount = s.Listeners.Count(l => l.IsConnected),
                StreamUrl = s.AzuraCastStation?.StreamUrl,
                StationShortcode = s.AzuraCastStation?.StationShortcode,
                PublicPlayerUrl = s.AzuraCastStation?.PublicPlayerUrl,
                ThumbnailUrl = s.ThumbnailUrl,
                Genre = s.Genre,
                CreatedAt = s.CreatedAt
            };
        }).ToList();

        var pagedResult = PagedResult<LiveSessionResult>.CreateUnpaged(results);
        return Result<PagedResult<LiveSessionResult>>.Success(pagedResult);
    }
}
