using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results.NowPlaying;
using LiveSessionService.Application.Features.Results.Stations;
using LiveSessionService.Application.Features.Stations.Commands.CreateStation;
using LiveSessionService.Application.Features.Stations.Commands.SyncStations;
using LiveSessionService.Application.Features.Stations.Queries.GetAllStations;
using LiveSessionService.Application.Features.Stations.Queries.GetStationNowPlaying;
using LiveSessionService.Api.Models.Responses;
using LiveSessionService.Api.Models.Requests.Stations;
using LiveSessionService.Api.Extensions;

namespace LiveSessionService.Api.Controllers;

/// <summary>
/// API endpoints for station management
/// All stations are synced from AzuraCast to local database
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class StationController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public StationController(ICommandDispatcher commands, IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    /// <summary>
    /// Get all stations from local database
    /// </summary>
    /// <response code="200">Returns list of stations</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StationResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> GetStations(CancellationToken ct)
    {
        var query = new GetAllStationsQuery();
        var result = await _queries.Send<GetAllStationsQuery, List<StationResult>>(query, ct);

        if (!result.IsSuccess)
        {
            return StatusCode(
                (int)(result.ErrorCode ?? ErrorCode.InternalServerError),
                result.ToApiResponse());
        }

        // check data counts
        var count = result.Data!.Count;
        var message = count == 0
            ? "No stations found. Please run POST /api/v1/station/sync to sync from AzuraCast."
            : $"Retrieved {count} station(s)";

        return Ok(ApiResponse<List<StationResult>>.SuccessResponse(result.Data!, message));
    }

    /// <summary>
    /// Create a new station
    /// </summary>
    /// <remarks>
    /// Creates a station in the local database.
    /// Note: This currently creates station locally only.
    /// Future enhancement: Will also create station in AzuraCast.
    /// </remarks>
    /// <response code="201">Station created successfully</response>
    /// <response code="400">Invalid input</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Admin role required</response>
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<StationResult>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> CreateStation(
        [FromBody] CreateStationRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        try
        {
            var command = new CreateStationCommand(
                request.StationName,
                request.Description,
                request.ShortCode);

            var result = await _commands.Send<CreateStationCommand, StationResult>(command, ct);

            if (!result.IsSuccess)
            {
                var statusCode = (int)(result.ErrorCode ?? ErrorCode.InternalServerError);
                return StatusCode(statusCode, result.ToApiResponse());
            }

            return CreatedAtAction(
                nameof(GetStations),
                new { id = result.Data!.Id },
                ApiResponse<StationResult>.SuccessResponse(
                    result.Data!,
                    "Station created successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.FailureResponse(
                "Failed to create station",
                500));
        }
    }

    /// <summary>
    /// Get live now-playing data from AzuraCast for a station (not a live session)
    /// </summary>
    /// <param name="id">Station local Guid (from GET /station)</param>
    [HttpGet("{id:guid}/now-playing")]
    [ProducesResponseType(typeof(ApiResponse<StationNowPlayingResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetNowPlaying(Guid id, CancellationToken ct)
    {
        var query = new GetStationNowPlayingQuery(id);
        var result = await _queries.Send<GetStationNowPlayingQuery, StationNowPlayingResult>(query, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<StationNowPlayingResult>.SuccessResponse(result.Data!, "Success"));
    }

    /// <summary>
    /// Restarts the AzuraCast station broadcast
    /// </summary>
    /// <param name="id">Station local Guid</param>
    [HttpPost("{id:guid}/restart")]
    [Authorize(Roles = "ADMIN,STAFF")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Restart(Guid id, CancellationToken ct)
    {
        var command = new Application.Features.Stations.Commands.RestartStation.RestartStationCommand(id);
        var result = await _commands.Send<Application.Features.Stations.Commands.RestartStation.RestartStationCommand, bool>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Station restart requested"));
    }

    /// <summary>
    /// Reloads the AzuraCast station broadcast config
    /// </summary>
    /// <param name="id">Station local Guid</param>
    [HttpPost("{id:guid}/reload")]
    [Authorize(Roles = "ADMIN,STAFF")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Reload(Guid id, CancellationToken ct)
    {
        var command = new Application.Features.Stations.Commands.ReloadStation.ReloadStationCommand(id);
        var result = await _commands.Send<Application.Features.Stations.Commands.ReloadStation.ReloadStationCommand, bool>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Station reload requested"));
    }

    /// <summary>
    /// Sync all stations from AzuraCast to local database
    /// </summary>
    /// <response code="200">Sync completed with statistics</response>
    /// <response code="401">Authentication failed - invalid API key</response>
    /// <response code="404">No stations found in AzuraCast</response>
    /// <response code="503">Cannot connect to AzuraCast</response>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(ApiResponse<SyncStationsResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 503)]
    public async Task<IActionResult> SyncStations(CancellationToken ct)
    {
        var command = new SyncStationsCommand();
        var result = await _commands.Send<SyncStationsCommand, SyncStationsResult>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.Unauthorized => Unauthorized(result.ToApiResponse()),
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.ServiceUnavailable => StatusCode(503, result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        // check data counts
        var data = result.Data!;
        var message = data.FailedStations > 0
            ? $"Sync completed with warnings: {data.CreatedStations} created, {data.UpdatedStations} updated, {data.FailedStations} failed. Check 'errors' field for details."
            : $"Sync completed successfully: {data.CreatedStations} created, {data.UpdatedStations} updated";

        return Ok(ApiResponse<SyncStationsResult>.SuccessResponse(data, message));
    }
}
