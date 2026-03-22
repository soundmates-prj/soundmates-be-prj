using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetAllSessionSchedules;

public sealed class GetAllSessionSchedulesHandler : IQueryHandler<GetAllSessionSchedulesQuery, List<SessionScheduleResult>>
{
    private readonly ISessionScheduleRepository _scheduleRepository;

    public GetAllSessionSchedulesHandler(ISessionScheduleRepository scheduleRepository)
    {
        _scheduleRepository = scheduleRepository;
    }

    public async Task<Result<List<SessionScheduleResult>>> Handle(
        GetAllSessionSchedulesQuery query,
        CancellationToken cancellationToken)
    {
        var schedules = query.LiveSessionId.HasValue
            ? await _scheduleRepository.GetByLiveSessionIdAsync(query.LiveSessionId.Value, cancellationToken)
            : await _scheduleRepository.GetAllAsync(cancellationToken);

        var results = schedules.Select(x => new SessionScheduleResult
        {
            Id = x.Id,
            LiveSessionId = x.LiveSessionId,
            StartTime = x.StartTime,
            EndTime = x.EndTime,
            Title = x.Title,
            Status = x.Status
        }).ToList();

        return Result<List<SessionScheduleResult>>.Success(results);
    }
}
