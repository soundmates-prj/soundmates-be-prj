using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetScheduleById;

public sealed class GetScheduleByIdHandler : IQueryHandler<GetScheduleByIdQuery, SessionScheduleResult>
{
    private readonly ISessionScheduleRepository _scheduleRepository;

    public GetScheduleByIdHandler(ISessionScheduleRepository scheduleRepository)
    {
        _scheduleRepository = scheduleRepository;
    }

    public async Task<Result<SessionScheduleResult>> Handle(GetScheduleByIdQuery query, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleRepository.GetByIdWithSessionAsync(query.ScheduleId, cancellationToken);

        if (schedule == null)
        {
            return Result<SessionScheduleResult>.Failure("Schedule not found", ErrorCode.NotFound);
        }

        return Result<SessionScheduleResult>.Success(MapToResult(schedule));
    }

    private static SessionScheduleResult MapToResult(Domain.Entities.SessionSchedule x) => new()
    {
        Id = x.Id,
        LiveSessionId = x.LiveSessionId,
        StartTime = x.StartTime,
        EndTime = x.EndTime,
        Title = x.Title,
        Status = x.Status.ToString(),
        IsRecurring = x.IsRecurring,
        DaysOfWeek = x.DaysOfWeek,
        StartDate = x.StartDate,
        EndDate = x.EndDate,
        CreatedBy = x.CreatedBy,
        UpdatedBy = x.UpdatedBy,
        CreatedAt = x.CreatedAt,
        LiveSession = x.LiveSession != null ? new LiveSessionScheduleData
        {
            Id = x.LiveSession.Id,
            SessionName = x.LiveSession.SessionName,
            Description = x.LiveSession.Description,
            Status = x.LiveSession.Status.ToString(),
            HostUserId = x.LiveSession.HostUserId,
            StartedAt = x.LiveSession.StartedAt,
            EndedAt = x.LiveSession.EndedAt,
            Genre = x.LiveSession.Genre,
            ThumbnailUrl = x.LiveSession.ThumbnailUrl,
            Station = x.LiveSession.AzuraCastStation != null ? new StationScheduleData
            {
                Id = x.LiveSession.AzuraCastStation.Id,
                ExternalStationId = x.LiveSession.AzuraCastStation.ExternalStationId,
                StationName = x.LiveSession.AzuraCastStation.StationName,
                StationShortcode = x.LiveSession.AzuraCastStation.StationShortcode,
                Description = x.LiveSession.AzuraCastStation.Description,
                StreamUrl = x.LiveSession.AzuraCastStation.StreamUrl,
                PublicPlayerUrl = x.LiveSession.AzuraCastStation.PublicPlayerUrl
            } : null
        } : null
    };
}
