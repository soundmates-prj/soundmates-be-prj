using AuthQueryService.Application.Abstractions.Messaging;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Domain.Interfaces;

namespace AuthQueryService.Application.ActivityLogs.Queries.GetUserActivityLogs
{
    public sealed class GetUserActivityLogsQueryHandler 
        : IQueryHandler<GetUserActivityLogsQuery, List<UserActivityLogDto>>
    {
        private readonly IUserActivityLogRepository _repo;

        public GetUserActivityLogsQueryHandler(IUserActivityLogRepository repo) => _repo = repo;

        public async Task<ApiResponse<List<UserActivityLogDto>>> Handle(
            GetUserActivityLogsQuery query, 
            CancellationToken cancellationToken)
        {
            var logs = await _repo.GetByUserIdAsync(query.UserId, query.Limit);
            
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

            return ApiResponse<List<UserActivityLogDto>>.SuccessResponse(dtos);
        }
    }
}
