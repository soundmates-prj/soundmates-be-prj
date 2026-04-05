using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.SearchSchedules;

public sealed class SearchSchedulesQueryHandler
    : IQueryHandler<SearchSchedulesQuery, SearchSchedulesResult>
{
    private readonly ISessionScheduleRepository _scheduleRepository;
    private readonly ILogger<SearchSchedulesQueryHandler> _logger;

    public SearchSchedulesQueryHandler(
        ISessionScheduleRepository scheduleRepository,
        ILogger<SearchSchedulesQueryHandler> logger)
    {
        _scheduleRepository = scheduleRepository;
        _logger = logger;
    }

    public async Task<Result<SearchSchedulesResult>> Handle(
        SearchSchedulesQuery query,
        CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var size = query.PageSize <= 0 ? 20 : query.PageSize;

        _logger.LogInformation(
            "Searching schedules: Q={Q}, Status={Status}, Page={Page}, PageSize={PageSize}",
            query.Q ?? "(all)", query.Status?.ToString() ?? "all", page, size);

        var (schedules, totalCount) = await _scheduleRepository.SearchAsync(
            query.Q,
            query.Status,
            query.FromDate,
            query.ToDate,
            page,
            size,
            cancellationToken);

        var items = schedules.Select(x => new SessionScheduleResult
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
        }).ToList();

        var result = new SearchSchedulesResult(items, totalCount, page, size);
        return Result<SearchSchedulesResult>.Success(result);
    }
}
