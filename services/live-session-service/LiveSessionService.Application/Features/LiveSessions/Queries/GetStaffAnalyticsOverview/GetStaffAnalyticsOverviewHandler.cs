using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetStaffAnalyticsOverview;

public sealed record GetStaffAnalyticsOverviewQuery(int Days = 7) : IQuery<StaffAnalyticsOverviewResult>;

public sealed class GetStaffAnalyticsOverviewHandler : IQueryHandler<GetStaffAnalyticsOverviewQuery, StaffAnalyticsOverviewResult>
{
    private readonly ILiveSessionRepository _repository;

    public GetStaffAnalyticsOverviewHandler(ILiveSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<StaffAnalyticsOverviewResult>> Handle(GetStaffAnalyticsOverviewQuery request, CancellationToken cancellationToken)
    {
        var normalizedDays = request.Days <= 0 ? 7 : Math.Min(request.Days, 90);
        
        var overview = await _repository.GetStaffAnalyticsOverviewAsync(normalizedDays, cancellationToken);

        var result = new StaffAnalyticsOverviewResult
        {
            TotalSystemMusic = overview.TotalSystemMusic,
            TotalStorageBytes = overview.TotalStorageBytes,
            TotalStations = overview.TotalStations,
            PendingSongRequests = overview.PendingSongRequests,
            ContentGrowthChart = overview.ContentGrowthChart.Select(x => new DailyContentGrowthResult
            {
                Date = x.Date.ToString("yyyy-MM-dd"),
                NewMusicCount = x.NewMusicCount
            }).ToList(),
            ModerationChart = overview.ModerationChart.Select(x => new DailyModerationResult
            {
                Date = x.Date.ToString("yyyy-MM-dd"),
                PendingCount = x.PendingCount,
                ResolvedCount = x.ResolvedCount
            }).ToList()
        };

        return Result<StaffAnalyticsOverviewResult>.Success(result);
    }
}
