using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetHostAnalyticsOverview;

public sealed record GetHostAnalyticsOverviewQuery(Guid HostUserId, int Days, bool IsStaffOrAdmin = false) : IQuery<HostAnalyticsOverviewResult>;
