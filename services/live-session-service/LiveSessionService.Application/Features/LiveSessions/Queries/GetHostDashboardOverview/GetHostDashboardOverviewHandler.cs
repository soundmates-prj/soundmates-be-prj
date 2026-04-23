using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Application.Features.Results.SongRequests;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetHostDashboardOverview;

public sealed class GetHostDashboardOverviewHandler : IQueryHandler<GetHostDashboardOverviewQuery, HostDashboardOverviewResult>
{
    private readonly ILiveSessionRepository _liveSessionRepository;
    private readonly ISessionScheduleRepository _scheduleRepository;

    public GetHostDashboardOverviewHandler(
        ILiveSessionRepository liveSessionRepository,
        ISessionScheduleRepository scheduleRepository)
    {
        _liveSessionRepository = liveSessionRepository;
        _scheduleRepository = scheduleRepository;
    }

    public async Task<Result<HostDashboardOverviewResult>> Handle(
        GetHostDashboardOverviewQuery query,
        CancellationToken cancellationToken)
    {
        var overview = await _liveSessionRepository.GetHostDashboardOverviewAsync(query.HostUserId, query.Days, cancellationToken);
        
        // Also fetch upcoming schedules for the host
        var hostLiveSessions = await _liveSessionRepository.GetByHostUserIdAsync(query.HostUserId, cancellationToken);
        var hostLiveSessionIds = hostLiveSessions.Select(x => x.Id).ToList();
        
        var schedules = await _scheduleRepository.GetAllAsync(cancellationToken);
        var upcomingSchedules = schedules
            .Where(x => hostLiveSessionIds.Contains(x.LiveSessionId))
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.StartTime)
            .Take(5)
            .ToList();

        var upcomingScheduleResults = upcomingSchedules.Select(s => new SessionScheduleResult
        {
            Id = s.Id,
            LiveSessionId = s.LiveSessionId,
            Title = s.Title,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Status = s.Status.ToString(),
            IsRecurring = s.IsRecurring,
            DaysOfWeek = s.DaysOfWeek,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            CreatedBy = s.CreatedBy,
            UpdatedBy = s.UpdatedBy,
            CreatedAt = s.CreatedAt
        }).ToList();

        var result = new HostDashboardOverviewResult
        {
            TotalSessions = overview.TotalSessions,
            TotalListeners = overview.TotalListeners,
            PendingMusicRequests = overview.PendingMusicRequests,
            ChartData = overview.ChartData.Select(x => new DailyHostStatResult
            {
                Date = x.Date.ToString("yyyy-MM-dd"),
                SessionsCount = x.SessionsCount,
                ListenersCount = x.ListenersCount
            }).ToList(),
            UpcomingSchedules = upcomingScheduleResults,
            RecentMusicRequests = overview.RecentMusicRequests.Select(x => new SongRequestResult
            {
                Id = x.Id,
                LiveSessionId = x.LiveSessionId,
                MediaFileId = x.MediaFileId,
                RequestedByUserId = x.RequestedByUserId,
                Status = x.Status.ToString(),
                ReviewedByUserId = x.ReviewedByUserId,
                RequestedAt = x.RequestedAt,
                ReviewedAt = x.ReviewedAt,
                Message = x.Message,
                RejectReason = x.RejectReason,
                SongTitle = x.MediaFile?.Title ?? "Unknown Title",
                SongArtist = x.MediaFile?.Artist,
                SongAlbum = x.MediaFile?.Album
            }).ToList(),
            EndedSessionsAnalysis = overview.EndedSessionsAnalysis.Select(x => new EndedSessionAnalysisResult
            {
                SessionId = x.SessionId,
                SessionName = x.SessionName,
                EndedAt = x.EndedAt,
                TotalDurationMinutes = x.TotalDurationMinutes,
                TotalListeners = x.TotalListeners,
                MusicRequestsCount = x.MusicRequestsCount
            }).ToList()
        };

        return Result<HostDashboardOverviewResult>.Success(result);
    }
}
