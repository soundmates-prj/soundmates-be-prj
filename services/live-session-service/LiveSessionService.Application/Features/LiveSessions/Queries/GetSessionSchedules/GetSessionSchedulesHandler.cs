using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetSessionSchedules;

public sealed class GetSessionSchedulesHandler : IQueryHandler<GetSessionSchedulesQuery, List<SessionScheduleResult>>
{
    private readonly ILiveSessionRepository _sessionRepository;
    private readonly ISessionScheduleRepository _scheduleRepository;

    public GetSessionSchedulesHandler(
        ILiveSessionRepository sessionRepository,
        ISessionScheduleRepository scheduleRepository)
    {
        _sessionRepository = sessionRepository;
        _scheduleRepository = scheduleRepository;
    }

    public async Task<Result<List<SessionScheduleResult>>> Handle(
        GetSessionSchedulesQuery query,
        CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(query.LiveSessionId, cancellationToken);
        if (session == null)
        {
            return Result<List<SessionScheduleResult>>.Failure("Session not found", ErrorCode.NotFound);
        }

        var schedules = await _scheduleRepository.GetByLiveSessionIdAsync(query.LiveSessionId, cancellationToken);

        var results = schedules
            .Select(x => new SessionScheduleResult
            {
                Id = x.Id,
                LiveSessionId = x.LiveSessionId,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Title = x.Title,
                Status = x.Status
            })
            .ToList();

        return Result<List<SessionScheduleResult>>.Success(results);
    }
}
