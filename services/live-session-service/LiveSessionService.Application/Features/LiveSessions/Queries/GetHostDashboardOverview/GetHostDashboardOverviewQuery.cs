using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetHostDashboardOverview;

public sealed record GetHostDashboardOverviewQuery(Guid HostUserId, int Days = 7) : IQuery<HostDashboardOverviewResult>;
