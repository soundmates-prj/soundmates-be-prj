using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetStaffDashboardOverview;

public sealed class GetStaffDashboardOverviewHandler : IQueryHandler<GetStaffDashboardOverviewQuery, StaffDashboardOverviewResult>
{
    private readonly ILiveSessionRepository _liveSessionRepository;
    private readonly IAzuraCastStationRepository _stationRepository;

    public GetStaffDashboardOverviewHandler(
        ILiveSessionRepository liveSessionRepository,
        IAzuraCastStationRepository stationRepository)
    {
        _liveSessionRepository = liveSessionRepository;
        _stationRepository = stationRepository;
    }

    public async Task<Result<StaffDashboardOverviewResult>> Handle(
        GetStaffDashboardOverviewQuery query,
        CancellationToken cancellationToken)
    {
        var normalizedDays = query.Days <= 0 ? 7 : Math.Min(query.Days, 90);
        var utcToday = DateTime.UtcNow.Date;

        var stationsTask = _stationRepository.GetAllEnabledAsync(cancellationToken);
        var dashboardTask = _liveSessionRepository.GetStaffDashboardOverviewAsync(normalizedDays, cancellationToken);

        await Task.WhenAll(stationsTask, dashboardTask);

        var stations = stationsTask.Result;
        var dashboard = dashboardTask.Result;

        var result = new StaffDashboardOverviewResult
        {
            TotalStations = stations.Count,
            StationsCreatedToday = stations.Count(x => x.CreatedAt.Date == utcToday),
            TotalSessions = dashboard.TotalSessions,
            LiveSessions = dashboard.LiveSessions,
            ListenersToday = dashboard.ListenersToday,
            DailyListeners = dashboard.DailyListeners
                .Select(x => new DailyListenerPointResult
                {
                    Date = x.Date.ToString("yyyy-MM-dd"),
                    ListenerCount = x.ListenerCount
                })
                .ToList()
        };

        return Result<StaffDashboardOverviewResult>.Success(result);
    }
}
