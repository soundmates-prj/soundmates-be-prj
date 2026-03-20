using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results.LiveSessions;

namespace LiveSessionService.Application.Features.LiveSessions.Queries.GetStaffDashboardOverview;

public sealed record GetStaffDashboardOverviewQuery(int Days = 7) : IQuery<StaffDashboardOverviewResult>;
