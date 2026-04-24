using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Interfaces;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetAdminAnalyticsOverview;

public sealed class GetAdminAnalyticsOverviewHandler : IQueryHandler<GetAdminAnalyticsOverviewQuery, AdminAnalyticsOverviewResult>
{
    private readonly ILiveSessionRepository _repository;

    public GetAdminAnalyticsOverviewHandler(ILiveSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<AdminAnalyticsOverviewResult>> Handle(GetAdminAnalyticsOverviewQuery request, CancellationToken cancellationToken)
    {
        var overview = await _repository.GetAdminAnalyticsOverviewAsync(request.Days, cancellationToken);

        return Result<AdminAnalyticsOverviewResult>.Success(new AdminAnalyticsOverviewResult
        {
            TotalSessions = overview.TotalSessions,
            TotalViews = overview.TotalViews,
            TotalInteractions = overview.TotalInteractions,
            SessionGrowthChart = overview.SessionGrowthChart,
            ListenerGrowthChart = overview.ListenerGrowthChart,
            InteractionGrowthChart = overview.InteractionGrowthChart
        });
    }
}
