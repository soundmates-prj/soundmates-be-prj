using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthQueryService.Application.ActivityLogs.Queries.GetRecentActivityLogs
{
    /// <summary>
    /// Handler for retrieving recent activity logs across all users
    /// </summary>
    public sealed class GetRecentActivityLogsQueryHandler 
        : IQueryHandler<GetRecentActivityLogsQuery, List<UserActivityLogDto>>
    {
        private readonly IUserActivityLogRepository _repo;
        private readonly ILogger<GetRecentActivityLogsQueryHandler> _logger;

        public GetRecentActivityLogsQueryHandler(
            IUserActivityLogRepository repo,
            ILogger<GetRecentActivityLogsQueryHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<ApiResponse<List<UserActivityLogDto>>> Handle(
            GetRecentActivityLogsQuery query, 
            CancellationToken cancellationToken)
        {
            try
            {
                // Validate query parameters
                if (query.Limit < 1 || query.Limit > 1000)
                {
                    _logger.LogWarning("Invalid limit parameter: {Limit}", query.Limit);
                    return ApiResponse<List<UserActivityLogDto>>.FailureResponse(
                        "Limit must be between 1 and 1000", 400);
                }

                _logger.LogInformation("Fetching recent activity logs with limit: {Limit}", query.Limit);
                
                // Retrieve logs from repository
                var logs = await _repo.GetRecentActivitiesAsync(query.Limit);
                
                if (logs == null || !logs.Any())
                {
                    _logger.LogInformation("No activity logs found");
                    return ApiResponse<List<UserActivityLogDto>>.SuccessResponse(new List<UserActivityLogDto>());
                }

                // Map domain entities to DTOs
                var dtos = logs.Select(log => new UserActivityLogDto
                {
                    Id = log.Id,
                    UserId = log.UserId,
                    Username = log.Username,
                    Email = log.Email,
                    ActivityType = log.ActivityType,
                    EventType = log.EventType,
                    IsSuccess = log.IsSuccess,
                    Reason = log.Reason,
                    ErrorCode = log.ErrorCode,
                    IpAddress = log.IpAddress,
                    UserAgent = log.UserAgent,
                    Location = log.Location,
                    Metadata = log.Metadata,
                    OccurredAt = log.OccurredAt
                }).ToList();

                _logger.LogInformation("Successfully retrieved {Count} activity logs", dtos.Count);
                
                return ApiResponse<List<UserActivityLogDto>>.SuccessResponse(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving recent activity logs");
                return ApiResponse<List<UserActivityLogDto>>.FailureResponse(
                    "An error occurred while retrieving activity logs", 500);
            }
        }
    }
}
