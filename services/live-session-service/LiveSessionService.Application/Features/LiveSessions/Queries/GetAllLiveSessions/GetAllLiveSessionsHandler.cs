using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetAllLiveSessions;

public sealed class GetAllLiveSessionsHandler : IQueryHandler<GetAllLiveSessionsQuery, PagedResult<LiveSessionResult>>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ISessionScheduleRepository _scheduleRepository;

    public GetAllLiveSessionsHandler(
        ILiveSessionRepository sessionRepository,
        ISessionScheduleRepository scheduleRepository)
    {
        _sessionRepository = sessionRepository;
        _scheduleRepository = scheduleRepository;
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

        var latestSchedules = await _scheduleRepository.GetLatestByLiveSessionIdsAsync(scheduledIds, cancellationToken);

        var results = sessions.Select(s =>
        {
            latestSchedules.TryGetValue(s.Id, out var schedule);

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
                    ? schedule?.StartTime ?? s.StartedAt
                    : null,
                StartedAt = s.Status is SessionStatus.Live or SessionStatus.Paused or SessionStatus.Ended
                    ? s.StartedAt
                    : null,
                EndedAt = s.Status == SessionStatus.Ended
                    ? s.EndedAt
                    : null,
                CreatedAt = s.CreatedAt
            };
        }).ToList();

        var pagedResult = PagedResult<LiveSessionResult>.CreateUnpaged(results);
        return Result<PagedResult<LiveSessionResult>>.Success(pagedResult);
    }
}
