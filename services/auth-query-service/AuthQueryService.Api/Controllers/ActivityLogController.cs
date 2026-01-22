using AuthQueryService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthQueryService.Application.ActivityLogs.Queries.GetUserActivityLogs;
using AuthQueryService.Application.ActivityLogs.Queries.GetRecentActivityLogs;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthQueryService.Api.Controllers
{
    /// <summary>
    /// Activity Logs API - Manages user activity tracking and audit logs
    /// </summary>
    [ApiController]
    [Route("api/v1/activity-logs")]
    [Authorize]
    public class ActivityLogController : ControllerBase
    {
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly ILogger<ActivityLogController> _logger;

        public ActivityLogController(
            IQueryDispatcher queryDispatcher,
            ILogger<ActivityLogController> logger)
        {
            _queryDispatcher = queryDispatcher;
            _logger = logger;
        }

        /// <summary>
        /// Get activity logs for a specific user
        /// </summary>
        /// <param name="userId">The user ID to retrieve activity logs for</param>
        /// <param name="limit">Maximum number of logs to return (1-500)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of user activity logs</returns>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<List<UserActivityLogDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<UserActivityLogDto>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<List<UserActivityLogDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserActivityLogs(
            [FromRoute] Guid userId,
            [FromQuery] int limit = 50,
            CancellationToken ct = default)
        {
            // Validate limit parameter
            if (limit < 1 || limit > 500)
            {
                _logger.LogWarning("Invalid limit parameter: {Limit}. Must be between 1 and 500", limit);
                return BadRequest(ApiResponse<List<UserActivityLogDto>>.FailureResponse(
                    "Limit must be between 1 and 500", 400));
            }

            if (userId == Guid.Empty)
            {
                _logger.LogWarning("Invalid userId: {UserId}", userId);
                return BadRequest(ApiResponse<List<UserActivityLogDto>>.FailureResponse(
                    "Invalid user ID", 400));
            }

            _logger.LogInformation("Retrieving activity logs for user {UserId} with limit {Limit}", userId, limit);
            
            var query = new GetUserActivityLogsQuery(userId, limit);
            var result = await _queryDispatcher.Query(query, ct);
            
            if (!result.Success)
            {
                _logger.LogWarning("Activity logs not found for user {UserId}", userId);
                return NotFound(result);
            }
            
            return Ok(result);
        }

        /// <summary>
        /// Get recent activity logs across all users (Admin/System monitoring)
        /// </summary>
        /// <param name="limit">Maximum number of logs to return (1-1000)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of recent activity logs</returns>
        /// <remarks>
        /// This endpoint retrieves the most recent activity logs from all users.
        /// Intended for system monitoring and administrative purposes.
        /// </remarks>
        [HttpGet("recent")]
        [ProducesResponseType(typeof(ApiResponse<List<UserActivityLogDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<UserActivityLogDto>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetRecentActivityLogs(
            [FromQuery] int limit = 100,
            CancellationToken ct = default)
        {
            // Validate limit parameter
            if (limit < 1 || limit > 1000)
            {
                _logger.LogWarning("Invalid limit parameter: {Limit}. Must be between 1 and 1000", limit);
                return BadRequest(ApiResponse<List<UserActivityLogDto>>.FailureResponse(
                    "Limit must be between 1 and 1000", 400));
            }

            _logger.LogInformation("Retrieving recent activity logs with limit {Limit}", limit);
            
            var query = new GetRecentActivityLogsQuery(limit);
            var result = await _queryDispatcher.Query(query, ct);
            
            if (!result.Success)
            {
                _logger.LogError("Failed to retrieve recent activity logs");
                return StatusCode(500, result);
            }
            
            return Ok(result);
        }
    }
}
