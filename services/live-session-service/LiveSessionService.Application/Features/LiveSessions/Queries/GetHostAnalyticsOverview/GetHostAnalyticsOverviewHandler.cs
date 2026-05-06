using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetHostAnalyticsOverview;

public sealed class GetHostAnalyticsOverviewHandler : IQueryHandler<GetHostAnalyticsOverviewQuery, HostAnalyticsOverviewResult>
{
    private readonly ILiveSessionRepository _sessionRepository;

    public GetHostAnalyticsOverviewHandler(ILiveSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<HostAnalyticsOverviewResult>> Handle(GetHostAnalyticsOverviewQuery query, CancellationToken cancellationToken)
    {
        var overview = await _sessionRepository.GetHostAnalyticsOverviewAsync(query.HostUserId, query.Days, query.IsStaffOrAdmin, cancellationToken);

        var result = new HostAnalyticsOverviewResult
        {
            TotalListeners = overview.TotalListeners,
            TotalSessions = overview.TotalSessions,
            PeakListeners = overview.PeakListeners,
            TotalMusicRequests = overview.TotalMusicRequests,
            ChartData = overview.ChartData.Select(x => new DailyHostAnalyticsResult
            {
                Date = x.Date.ToString("yyyy-MM-dd"),
                ListenersCount = x.ListenersCount,
                RequestsCount = x.RequestsCount,
                ChatCount = x.ChatCount
            }).ToList(),
            TopRequestedSongs = overview.TopRequestedSongs.Select(x => new TopSongRequestResult
            {
                Rank = x.Rank,
                Title = x.Title,
                Artist = x.Artist,
                Count = x.Count
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

        return Result<HostAnalyticsOverviewResult>.Success(result);
    }
}
