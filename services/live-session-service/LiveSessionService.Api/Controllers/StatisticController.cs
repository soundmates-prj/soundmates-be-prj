using LiveSessionService.Api.Extensions;
using LiveSessionService.Api.Models.Responses;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSessionStatistics;
using LiveSessionService.Application.Features.Results.LiveSessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveSessionService.Api.Controllers;

/// <summary>
/// API endpoints for Live Session Statistics
/// </summary>
[ApiController]
[Route("api/v1/livesession")]
[Produces("application/json")]
[Authorize]
public sealed class StatisticController : ControllerBase
{
    private readonly IQueryDispatcher _queries;

    public StatisticController(IQueryDispatcher queries)
    {
        _queries = queries;
    }

    /// <summary>
    /// Get detailed statistics for a live session.
    /// </summary>
    [HttpGet("{liveSessionId:guid}/statistics")]
    [ProducesResponseType(typeof(ApiResponse<LiveSessionStatisticsResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetLiveSessionStatistics(Guid liveSessionId, CancellationToken ct)
    {
        var query = new GetLiveSessionStatisticsQuery(liveSessionId);
        var result = await _queries.Send<GetLiveSessionStatisticsQuery, LiveSessionStatisticsResult>(query, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<LiveSessionStatisticsResult>.SuccessResponse(
            result.Data!,
            "Session statistics retrieved"));
    }

    /// <summary>
    /// Get aggregated analytics overview for admin dashboard.
    /// </summary>
    [HttpGet("admin/overview")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<AdminAnalyticsOverviewResult>), 200)]
    public async Task<IActionResult> GetAdminAnalyticsOverview([FromQuery] int days = 7, CancellationToken ct = default)
    {
        var query = new LiveSessionService.Application.Features.LiveSessions.Queries.GetAdminAnalyticsOverview.GetAdminAnalyticsOverviewQuery(days);
        var result = await _queries.Send<LiveSessionService.Application.Features.LiveSessions.Queries.GetAdminAnalyticsOverview.GetAdminAnalyticsOverviewQuery, AdminAnalyticsOverviewResult>(query, ct);

        if (!result.IsSuccess)
        {
            return StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse());
        }

        return Ok(ApiResponse<AdminAnalyticsOverviewResult>.SuccessResponse(
            result.Data!,
            "Admin analytics overview retrieved"));
    }
}
