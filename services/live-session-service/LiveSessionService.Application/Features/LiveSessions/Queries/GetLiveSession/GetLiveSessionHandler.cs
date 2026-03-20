using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSession;

public sealed class GetLiveSessionHandler : IQueryHandler<GetLiveSessionQuery, LiveSessionResult>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ISessionScheduleRepository _scheduleRepository;

    public GetLiveSessionHandler(
        ILiveSessionRepository sessionRepository,
        ISessionScheduleRepository scheduleRepository)
    {
        _sessionRepository = sessionRepository;
        _scheduleRepository = scheduleRepository;
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

        var latestSchedule = session.Status == SessionStatus.Scheduled
            ? await _scheduleRepository.GetLatestByLiveSessionIdAsync(session.Id, cancellationToken)
            : null;

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
                ? latestSchedule?.StartTime ?? session.StartedAt
                : null,
            StartedAt = session.Status is SessionStatus.Live or SessionStatus.Paused or SessionStatus.Ended
                ? session.StartedAt
                : null,
            EndedAt = session.Status == SessionStatus.Ended
                ? session.EndedAt
                : null,
            CreatedAt = session.CreatedAt
        };

        return Result<LiveSessionResult>.Success(result);
    }
}
