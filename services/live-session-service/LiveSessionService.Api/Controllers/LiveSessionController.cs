using Microsoft.AspNetCore.Mvc;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.LiveSessions;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Application.Features.LiveSessions.Commands.CreateLiveSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.StartSession;
using LiveSessionService.Application.Features.LiveSessions.Commands.StopSession;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetLiveSession;
using LiveSessionService.Application.Features.LiveSessions.Queries.GetAllLiveSessions;
using LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlaying;
using LiveSessionService.Api.Models.Responses;
using LiveSessionService.Api.Models.Requests.LiveSessions;
using LiveSessionService.Api.Extensions;

namespace LiveSessionService.Api.Controllers;

/// <summary>
/// API endpoints for Live Session management
/// Clean controller following StationController pattern
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
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
    /// Create a new live session
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LiveSessionResult>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Create([FromBody] CreateLiveSessionRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        var command = new CreateLiveSessionCommand(
            request.UserId,
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
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<LiveSessionResult>.SuccessResponse(result.Data!, "Session started"));
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
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<LiveSessionResult>.SuccessResponse(result.Data!, "Session stopped"));
    }

    /// <summary>
    /// Get current now playing for a live session
    /// </summary>
    [HttpGet("{id:guid}/now-playing")]
    [ProducesResponseType(typeof(ApiResponse<NowPlayingResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetNowPlaying(Guid id, CancellationToken ct)
    {
        var query = new GetNowPlayingQuery(id);
        var result = await _queries.Send<GetNowPlayingQuery, NowPlayingResult>(query, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<NowPlayingResult>.SuccessResponse(result.Data!, "Success"));
    }

    /// <summary>
    /// Get listener statistics for a live session
    /// </summary>
    [HttpGet("{id:guid}/listeners")]
    [ProducesResponseType(typeof(ApiResponse<ListenerStatsResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetListeners(Guid id, CancellationToken ct)
    {
        // Get session
        var sessionQuery = new GetLiveSessionQuery(id);
        var sessionResult = await _queries.Send<GetLiveSessionQuery, LiveSessionResult>(sessionQuery, ct);

        if (!sessionResult.IsSuccess)
        {
            return NotFound(ApiResponse<object>.FailureResponse(
                "Session not found",
                (int)ErrorCode.NotFound));
        }

        // Get now playing
        var nowPlayingQuery = new GetNowPlayingQuery(id);
        var nowPlayingResult = await _queries.Send<GetNowPlayingQuery, NowPlayingResult>(nowPlayingQuery, ct);

        var listenerStats = new ListenerStatsResult
        {
            SessionId = id,
            CurrentListeners = nowPlayingResult.IsSuccess ? nowPlayingResult.Data!.ListenerCount : 0,
            PeakListeners = sessionResult.Data!.PeakListeners,
            TotalListeners = sessionResult.Data.TotalListeners
        };

        return Ok(ApiResponse<ListenerStatsResult>.SuccessResponse(
            listenerStats,
            "Listener statistics retrieved"));
    }
}
