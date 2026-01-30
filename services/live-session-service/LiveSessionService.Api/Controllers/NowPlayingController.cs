using Microsoft.AspNetCore.Mvc;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.NowPlaying.Commands.SyncNowPlaying;
using LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlaying;
using LiveSessionService.Application.Features.NowPlaying.Queries.GetNowPlayingHistory;
using LiveSessionService.Api.Models.Responses;

namespace LiveSessionService.Api.Controllers;

/// <summary>
/// API endpoints for now playing functionality
/// Uses CQRS pattern with Result types
/// </summary>
[ApiController]
[Route("api/v1/now-playing")]
[Produces("application/json")]
public class NowPlayingController : ControllerBase
{
    private readonly ICommandHandler<SyncNowPlayingCommand, NowPlayingResult> _syncHandler;
    private readonly IQueryHandler<GetNowPlayingQuery, NowPlayingResult> _getNowPlayingHandler;
    private readonly IQueryHandler<GetNowPlayingHistoryQuery, List<NowPlayingHistoryResult>> _getHistoryHandler;

    public NowPlayingController(
        ICommandHandler<SyncNowPlayingCommand, NowPlayingResult> syncHandler,
        IQueryHandler<GetNowPlayingQuery, NowPlayingResult> getNowPlayingHandler,
        IQueryHandler<GetNowPlayingHistoryQuery, List<NowPlayingHistoryResult>> getHistoryHandler)
    {
        _syncHandler = syncHandler;
        _getNowPlayingHandler = getNowPlayingHandler;
        _getHistoryHandler = getHistoryHandler;
    }

    /// <summary>
    /// Get current now playing for a session
    /// </summary>
    /// <param name="sessionId">Session ID (UUID format, e.g., 22222222-2222-2222-2222-222222222222)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Current now playing data</returns>
    /// <response code="200">Returns the current now playing data</response>
    /// <response code="404">Session not found or no data available</response>
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<NowPlayingResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<NowPlayingResult>), 404)]
    public async Task<IActionResult> GetNowPlaying(
        [FromRoute] Guid sessionId,
        CancellationToken ct)
    {
        var query = new GetNowPlayingQuery(sessionId);
        var result = await _getNowPlayingHandler.Handle(query, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                404 => NotFound(ApiResponse<NowPlayingResult>.FailureResponse(result.ErrorMessage ?? "Not found", 404)),
                _ => StatusCode(result.ErrorCode ?? 500, ApiResponse<NowPlayingResult>.FailureResponse(result.ErrorMessage ?? "Error", result.ErrorCode ?? 500))
            };
        }

        return Ok(ApiResponse<NowPlayingResult>.SuccessResponse(result.Data!, "Success"));
    }

    /// <summary>
    /// Force sync/refresh now playing from AzuraCast
    /// </summary>
    /// <param name="sessionId">Session ID (UUID format)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated now playing data</returns>
    /// <response code="200">Successfully synced and returns updated data</response>
    /// <response code="400">Invalid request or session not active</response>
    /// <response code="404">Session not found</response>
    [HttpPost("{sessionId:guid}/sync")]
    [ProducesResponseType(typeof(ApiResponse<NowPlayingResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<NowPlayingResult>), 400)]
    [ProducesResponseType(typeof(ApiResponse<NowPlayingResult>), 404)]
    public async Task<IActionResult> SyncNowPlaying(
        [FromRoute] Guid sessionId,
        CancellationToken ct)
    {
        var command = new SyncNowPlayingCommand(sessionId);
        var result = await _syncHandler.Handle(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                404 => NotFound(ApiResponse<NowPlayingResult>.FailureResponse(result.ErrorMessage ?? "Not found", 404)),
                400 => BadRequest(ApiResponse<NowPlayingResult>.FailureResponse(result.ErrorMessage ?? "Bad request", 400)),
                _ => StatusCode(result.ErrorCode ?? 500, ApiResponse<NowPlayingResult>.FailureResponse(result.ErrorMessage ?? "Error", result.ErrorCode ?? 500))
            };
        }

        return Ok(ApiResponse<NowPlayingResult>.SuccessResponse(result.Data!, "Now playing synced successfully"));
    }

    /// <summary>
    /// Get now playing history for a session
    /// </summary>
    /// <param name="sessionId">Session ID (UUID format)</param>
    /// <param name="limit">Maximum number of items (default: 50, max: 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of historical now playing entries</returns>
    /// <response code="200">Returns the history list</response>
    /// <response code="404">Session not found</response>
    [HttpGet("{sessionId:guid}/history")]
    [ProducesResponseType(typeof(ApiResponse<List<NowPlayingHistoryResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<List<NowPlayingHistoryResult>>), 404)]
    public async Task<IActionResult> GetHistory(
        [FromRoute] Guid sessionId,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        // Validate limit
        if (limit < 1) limit = 1;
        if (limit > 100) limit = 100;

        var query = new GetNowPlayingHistoryQuery(sessionId, limit);
        var result = await _getHistoryHandler.Handle(query, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                404 => NotFound(ApiResponse<List<NowPlayingHistoryResult>>.FailureResponse(result.ErrorMessage ?? "Not found", 404)),
                _ => StatusCode(result.ErrorCode ?? 500, ApiResponse<List<NowPlayingHistoryResult>>.FailureResponse(result.ErrorMessage ?? "Error", result.ErrorCode ?? 500))
            };
        }

        return Ok(ApiResponse<List<NowPlayingHistoryResult>>.SuccessResponse(result.Data!, "Success"));
    }
}
