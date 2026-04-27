using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetAdminAnalyticsOverview;

public record GetAdminAnalyticsOverviewQuery(int Days = 7) : IQuery<AdminAnalyticsOverviewResult>;
