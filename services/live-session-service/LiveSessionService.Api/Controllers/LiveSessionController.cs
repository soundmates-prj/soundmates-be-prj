using LiveSessionService.Api.Extensions;
using LiveSessionService.Api.Models.Requests.LiveSessions;
using LiveSessionService.Api.Models.Responses;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.LiveSessions.Commands.CreateLiveSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.CreateSessionSchedule;
using LiveSessionService.Application.Features.LiveSessions.Commands.PauseSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.ResumeSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.StartSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.StopSession;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetAllLiveSessions;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSession;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetSessionSchedules;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Application.Features.Results.SongRequests;
using LiveSessionService.Application.Features.SongRequests.Commands.CreateSongRequest;
using LiveSessionService.Application.Features.SongRequests.Commands.ReviewSongRequest;
using LiveSessionService.Application.Features.SongRequests.Queries.GetSongRequestsBySession;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LiveSessionService.Api.Controllers;

/// <summary>
/// API endpoints for Live Session management
/// Clean controller following StationController pattern
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class LiveSessionController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public LiveSessionController(ICommandDispatcher commands, IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    /// <summary>
    /// Get all live sessions with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<LiveSessionResult>>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? userId,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = new GetAllLiveSessionsQuery(userId, status, pageNumber, pageSize);
        var result = await _queries.Send<GetAllLiveSessionsQuery, PagedResult<LiveSessionResult>>(query, ct);

        if (!result.IsSuccess)
        {
            return StatusCode(
                (int)(result.ErrorCode ?? ErrorCode.InternalServerError),
                result.ToApiResponse());
        }

        return Ok(ApiResponse<PagedResult<LiveSessionResult>>.SuccessResponse(
            result.Data!,
            $"Retrieved {result.Data!.Items.Count} session(s)"));
    }

    /// <summary>
    /// Get a specific live session by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LiveSessionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetLiveSessionQuery(id);
        var result = await _queries.Send<GetLiveSessionQuery, LiveSessionResult>(query, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<LiveSessionResult>.SuccessResponse(result.Data!, "Success"));
    }

    /// <summary>
    /// Create a new live stream session
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LiveSessionResult>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Create([FromBody] CreateLiveSessionRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse(
                "Invalid or missing user token",
                (int)ErrorCode.Unauthorized));
        }

        var command = new CreateLiveSessionCommand(
            currentUserId,
            request.StationId,
            request.SessionName,
            request.Description);

        var result = await _commands.Send<CreateLiveSessionCommand, LiveSessionResult>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Data!.Id },
            ApiResponse<LiveSessionResult>.SuccessResponse(result.Data!, "Live session created"));
    }

    /// <summary>
    /// Create a new schedule for an existing live session
    /// </summary>
    [HttpPost("{id:guid}/schedules")]
    [ProducesResponseType(typeof(ApiResponse<LiveSessionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> CreateSchedule(Guid id, [FromBody] CreateSessionScheduleRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        var command = new CreateSessionScheduleCommand(
            id,
            request.StartTime,
            request.EndTime,
            request.Title);

        var result = await _commands.Send<CreateSessionScheduleCommand, LiveSessionResult>(command, ct);

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

        return Ok(ApiResponse<LiveSessionResult>.SuccessResponse(result.Data!, "Session scheduled"));
    }

    /// <summary>
    /// Get schedules of a live session
    /// </summary>
    [HttpGet("{id:guid}/schedules")]
    [ProducesResponseType(typeof(ApiResponse<List<SessionScheduleResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetSchedules(Guid id, CancellationToken ct)
    {
        var query = new GetSessionSchedulesQuery(id);
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
    /// Start a live session
    /// </summary>
    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(typeof(ApiResponse<LiveSessionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        var command = new StartSessionCommand(id);
        var result = await _commands.Send<StartSessionCommand, LiveSessionResult>(command, ct);

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

        return Ok(ApiResponse<LiveSessionResult>.SuccessResponse(result.Data!, "Session started"));
    }

    /// <summary>
    /// Pause a live session
    /// </summary>
    [HttpPost("{id:guid}/pause")]
    [ProducesResponseType(typeof(ApiResponse<LiveSessionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Pause(Guid id, CancellationToken ct)
    {
        var command = new PauseSessionCommand(id);
        var result = await _commands.Send<PauseSessionCommand, LiveSessionResult>(command, ct);

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

        return Ok(ApiResponse<LiveSessionResult>.SuccessResponse(result.Data!, "Session paused"));
    }

    /// <summary>
    /// Resume a paused live session
    /// </summary>
    [HttpPost("{id:guid}/resume")]
    [ProducesResponseType(typeof(ApiResponse<LiveSessionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Resume(Guid id, CancellationToken ct)
    {
        var command = new ResumeSessionCommand(id);
        var result = await _commands.Send<ResumeSessionCommand, LiveSessionResult>(command, ct);

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

        return Ok(ApiResponse<LiveSessionResult>.SuccessResponse(result.Data!, "Session resumed"));
    }

    /// <summary>
    /// Stop a live session
    /// </summary>
    [HttpPost("{id:guid}/stop")]
    [ProducesResponseType(typeof(ApiResponse<LiveSessionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Stop(Guid id, CancellationToken ct)
    {
        var command = new StopSessionCommand(id);
        var result = await _commands.Send<StopSessionCommand, LiveSessionResult>(command, ct);

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

        return Ok(ApiResponse<LiveSessionResult>.SuccessResponse(result.Data!, "Session stopped"));
    }

    /// <summary>
    /// Get listener statistics for a live stream session
    /// </summary>
    [HttpGet("{id:guid}/listeners")]
    [ProducesResponseType(typeof(ApiResponse<ListenerStatsResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetListeners(Guid id, CancellationToken ct)
    {
        var sessionQuery = new GetLiveSessionQuery(id);
        var sessionResult = await _queries.Send<GetLiveSessionQuery, LiveSessionResult>(sessionQuery, ct);

        if (!sessionResult.IsSuccess)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "Session not found",
                (int)ErrorCode.NotFound));
        }

        var listenerStats = new ListenerStatsResult
        {
            SessionId = id,
            CurrentListeners = 0,
            PeakListeners = sessionResult.Data!.PeakListeners,
            TotalListeners = sessionResult.Data.TotalListeners
        };

        return Ok(ApiResponse<ListenerStatsResult>.SuccessResponse(
            listenerStats,
            "Listener statistics retrieved"));
    }

    /// <summary>
    /// Create a new song request for a live session
    /// </summary>
    [HttpPost("{id:guid}/song-requests")]
    [ProducesResponseType(typeof(ApiResponse<SongRequestResult>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> CreateSongRequest(
        Guid id,
        [FromBody] CreateSongRequestRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub")
            ?? User.FindFirst("user_id");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var requestedByUserId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse(
                "Invalid or missing user token",
                (int)ErrorCode.Unauthorized));
        }

        var command = new CreateSongRequestCommand(
            id,
            request.MediaFileId,
            requestedByUserId,
            request.Message);

        var result = await _commands.Send<CreateSongRequestCommand, SongRequestResult>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return StatusCode(201, result.ToApiResponse());
    }

    /// <summary>
    /// Get song requests of a live session
    /// </summary>
    [HttpGet("{id:guid}/song-requests")]
    [ProducesResponseType(typeof(ApiResponse<List<SongRequestResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetSongRequests(
        Guid id,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var query = new GetSongRequestsBySessionQuery(id, status);
        var result = await _queries.Send<GetSongRequestsBySessionQuery, List<SongRequestResult>>(query, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Approve or reject a song request
    /// </summary>
    [HttpPost("song-requests/{songRequestId:guid}/review")]
    [ProducesResponseType(typeof(ApiResponse<SongRequestResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> ReviewSongRequest(
        Guid songRequestId,
        [FromBody] ReviewSongRequestRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse(
                "Invalid or missing user token",
                (int)ErrorCode.Unauthorized));
        }

        var isApproved = request.Action.Equals("approve", StringComparison.OrdinalIgnoreCase);
        var isRejected = request.Action.Equals("reject", StringComparison.OrdinalIgnoreCase);

        if (!isApproved && !isRejected)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Action must be either 'approve' or 'reject'",
                (int)ErrorCode.BadRequest));
        }

        if (isRejected && string.IsNullOrWhiteSpace(request.RejectReason))
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Reject reason is required when action is reject",
                (int)ErrorCode.BadRequest));
        }

        var command = new ReviewSongRequestCommand(
            songRequestId,
            currentUserId,
            isApproved,
            request.RejectReason);

        var result = await _commands.Send<ReviewSongRequestCommand, SongRequestResult>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(result.ToApiResponse());
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
