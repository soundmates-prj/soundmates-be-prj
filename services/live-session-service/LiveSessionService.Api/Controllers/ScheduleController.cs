using LiveSessionService.Api.Extensions;
using LiveSessionService.Api.Models.Requests.LiveSessions;
using LiveSessionService.Api.Models.Responses;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.LiveSessions.Commands.CreateSessionSchedule;
using LiveSessionService.Application.Features.LiveSessions.Commands.DeleteSessionSchedule;
using LiveSessionService.Application.Features.LiveSessions.Commands.UpdateSessionSchedule;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetAllSessionSchedules;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetScheduleById;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetSessionSchedules;
using LiveSessionService.Application.Features.LiveSessions.Queries.SearchSchedules;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LiveSessionService.Api.Controllers;

/// <summary>
/// API endpoints for Live session schedules management.
/// GET endpoints are publicly accessible; write endpoints require authentication.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ScheduleController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public ScheduleController(ICommandDispatcher commands, IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    /// <summary>
    /// Get all session schedules — publicly accessible.
    /// Optionally filter by liveSessionId.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<SessionScheduleResult>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? liveSessionId, CancellationToken ct)
    {
        var query = new GetAllSessionSchedulesQuery(liveSessionId);
        var result = await _queries.Send<GetAllSessionSchedulesQuery, List<SessionScheduleResult>>(query, ct);

        if (!result.IsSuccess)
        {
            return StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse());
        }

        return Ok(ApiResponse<List<SessionScheduleResult>>.SuccessResponse(result.Data!, "Schedules retrieved"));
    }

    /// <summary>
    /// Get a single schedule by its ID — publicly accessible.
    /// </summary>
    [HttpGet("{scheduleId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SessionScheduleResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(Guid scheduleId, CancellationToken ct)
    {
        var query = new GetScheduleByIdQuery(scheduleId);
        var result = await _queries.Send<GetScheduleByIdQuery, SessionScheduleResult>(query, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<SessionScheduleResult>.SuccessResponse(result.Data!, "Schedule retrieved"));
    }

    /// <summary>
    /// Get schedules of a live session — publicly accessible.
    /// </summary>
    [HttpGet("live-session/{liveSessionId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<SessionScheduleResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetByLiveSessionId(Guid liveSessionId, CancellationToken ct)
    {
        var query = new GetSessionSchedulesQuery(liveSessionId);
        var result = await _queries.Send<GetSessionSchedulesQuery, List<SessionScheduleResult>>(query, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<List<SessionScheduleResult>>.SuccessResponse(result.Data!, "Schedules retrieved"));
    }

    /// <summary>
    /// Create a new schedule for an existing live session — requires authentication.
    /// </summary>
    [HttpPost("live-session/{liveSessionId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<SessionScheduleResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Create(Guid liveSessionId, [FromBody] CreateSessionScheduleRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse("Invalid input", (int)ErrorCode.BadRequest));
        }

        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse(
                "Invalid or missing user token",
                (int)ErrorCode.Unauthorized));
        }

        var command = new CreateSessionScheduleCommand(
            liveSessionId,
            request.StartTime,
            request.EndTime,
            request.StartDate,
            request.EndDate,
            request.Title,
            currentUserId,
            request.IsRecurring,
            request.DaysOfWeek);

        var result = await _commands.Send<CreateSessionScheduleCommand, SessionScheduleResult>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                ErrorCode.Conflict => Conflict(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<SessionScheduleResult>.SuccessResponse(result.Data!, "Session scheduled"));
    }

    /// <summary>
    /// Update a session schedule — requires authentication.
    /// </summary>
    [HttpPut("{scheduleId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<SessionScheduleResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Update(Guid scheduleId, [FromBody] UpdateSessionScheduleRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse("Invalid input", (int)ErrorCode.BadRequest));
        }

        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse(
                "Invalid or missing user token",
                (int)ErrorCode.Unauthorized));
        }

        var command = new UpdateSessionScheduleCommand(
            scheduleId,
            request.StartTime,
            request.EndTime,
            request.StartDate,
            request.EndDate,
            request.Title,
            currentUserId,
            request.IsRecurring,
            request.DaysOfWeek);

        var result = await _commands.Send<UpdateSessionScheduleCommand, SessionScheduleResult>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                ErrorCode.Conflict => Conflict(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<SessionScheduleResult>.SuccessResponse(result.Data!, "Schedule updated"));
    }

    /// <summary>
    /// Delete a session schedule — requires authentication.
    /// </summary>
    [HttpDelete("{scheduleId:guid}")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Delete(Guid scheduleId, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse(
                "Invalid or missing user token",
                (int)ErrorCode.Unauthorized));
        }

        var command = new DeleteSessionScheduleCommand(scheduleId, currentUserId);
        var result = await _commands.Send(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Search session schedules by keyword with optional filters.
    /// Public endpoint — for global search bar.
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PageResponse<SessionScheduleResult>>), 200)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Parse status enum if provided
        ScheduleStatus? scheduleStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ScheduleStatus>(status, true, out var parsed))
        {
            scheduleStatus = parsed;
        }

        var query = new SearchSchedulesQuery(q, scheduleStatus, fromDate, toDate, page, pageSize);
        var result = await _queries.Send<SearchSchedulesQuery, SearchSchedulesResult>(query, ct);

        if (!result.IsSuccess)
        {
            return StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse());
        }

        var pageResponse = new PageResponse<SessionScheduleResult>
        {
            Content = result.Data!.Items,
            Page = result.Data.PageNumber,
            Size = result.Data.PageSize,
            TotalElements = result.Data.TotalCount,
            TotalPages = result.Data.TotalPages
        };

        return Ok(ApiResponse<PageResponse<SessionScheduleResult>>.SuccessResponse(pageResponse, "Schedules search completed"));
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub")
            ?? User.FindFirst("user_id");

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedUserId))
        {
            userId = parsedUserId;
            return true;
        }

        userId = Guid.Empty;
        return false;
    }
}
