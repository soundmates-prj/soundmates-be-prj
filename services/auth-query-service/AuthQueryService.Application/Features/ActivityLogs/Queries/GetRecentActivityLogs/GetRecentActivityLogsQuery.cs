using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using System.ComponentModel.DataAnnotations;

namespace AuthQueryService.Application.Services.ActivityLogs.Queries.GetRecentActivityLogs
{
    /// <summary>
    /// Query to retrieve recent activity logs across all users
    /// Used for system-wide monitoring and audit purposes
    /// </summary>
    /// <param name="Limit">Maximum number of logs to return (1-1000, default: 100)</param>
    public sealed record GetRecentActivityLogsQuery(
        [Range(1, 1000, ErrorMessage = "Limit must be between 1 and 1000")]
        int Limit = 100
    ) : IQuery<List<UserActivityLogDto>>;
}
