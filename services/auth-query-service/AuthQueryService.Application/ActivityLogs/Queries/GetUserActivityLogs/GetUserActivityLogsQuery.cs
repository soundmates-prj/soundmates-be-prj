using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;

namespace AuthQueryService.Application.ActivityLogs.Queries.GetUserActivityLogs
{
    public sealed record GetUserActivityLogsQuery(Guid UserId, int Limit = 50) : IQuery<List<UserActivityLogDto>>;
}
