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
                Status = x.Status.ToString(),
                IsRecurring = x.IsRecurring,
                DaysOfWeek = x.DaysOfWeek,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                CreatedBy = x.CreatedBy,
                UpdatedBy = x.UpdatedBy,
                CreatedByUserId = x.CreatedBy,
                UpdatedByUserId = x.UpdatedBy,
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
            })
            .ToList();

        return Result<List<SessionScheduleResult>>.Success(results);
    }
}
