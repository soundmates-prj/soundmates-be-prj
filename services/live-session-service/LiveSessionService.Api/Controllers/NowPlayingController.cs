using Microsoft.AspNetCore.Mvc;
using LiveSessionService.Application.Abstractions.Messaging;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results;
using LiveSessionService.Application.Features.Results.NowPlaying;
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
    private readonly IQueryHandler<GetNowPlayingHistoryQuery, PagedResult<NowPlayingHistoryResult>> _getHistoryHandler;

    public NowPlayingController(
        ICommandHandler<SyncNowPlayingCommand, NowPlayingResult> syncHandler,
        IQueryHandler<GetNowPlayingQuery, NowPlayingResult> getNowPlayingHandler,
        IQueryHandler<GetNowPlayingHistoryQuery, PagedResult<NowPlayingHistoryResult>> getHistoryHandler)
    {
        _syncHandler = syncHandler;
        _getNowPlayingHandler = getNowPlayingHandler;
        _getHistoryHandler = getHistoryHandler;
    }

    /// <summary>
    /// Get current now playing for a session
    /// </summary>
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
                ErrorCode.NotFound => NotFound(ApiResponse<NowPlayingResult>.FailureResponse(
                    result.ErrorMessage ?? "Not found", 
                    (int)ErrorCode.NotFound)),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), 
                    ApiResponse<NowPlayingResult>.FailureResponse(
                        result.ErrorMessage ?? "Error", 
                        (int)(result.ErrorCode ?? ErrorCode.InternalServerError)))
            };
        }

        return Ok(ApiResponse<NowPlayingResult>.SuccessResponse(result.Data!, "Success"));
    }

    /// <summary>
    /// Force sync/refresh now playing from AzuraCast
    /// </summary>
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
                ErrorCode.NotFound => NotFound(ApiResponse<NowPlayingResult>.FailureResponse(
                    result.ErrorMessage ?? "Not found", 
                    (int)ErrorCode.NotFound)),
                ErrorCode.BadRequest => BadRequest(ApiResponse<NowPlayingResult>.FailureResponse(
                    result.ErrorMessage ?? "Bad request", 
                    (int)ErrorCode.BadRequest)),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), 
                    ApiResponse<NowPlayingResult>.FailureResponse(
                        result.ErrorMessage ?? "Error", 
                        (int)(result.ErrorCode ?? ErrorCode.InternalServerError)))
            };
        }

        return Ok(ApiResponse<NowPlayingResult>.SuccessResponse(result.Data!, "Now playing synced successfully"));
    }

    /// <summary>
    /// Get now playing history for a session (with pagination support)
    /// </summary>
    [HttpGet("{sessionId:guid}/history")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<NowPlayingHistoryResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<NowPlayingHistoryResult>>), 404)]
    public async Task<IActionResult> GetHistory(
        [FromRoute] Guid sessionId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        // Validate pagination params
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var query = new GetNowPlayingHistoryQuery(sessionId, pageNumber, pageSize);
        var result = await _getHistoryHandler.Handle(query, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(ApiResponse<PagedResult<NowPlayingHistoryResult>>.FailureResponse(
                    result.ErrorMessage ?? "Not found", 
                    (int)ErrorCode.NotFound)),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), 
                    ApiResponse<PagedResult<NowPlayingHistoryResult>>.FailureResponse(
                        result.ErrorMessage ?? "Error", 
                        (int)(result.ErrorCode ?? ErrorCode.InternalServerError)))
            };
        }

        return Ok(ApiResponse<PagedResult<NowPlayingHistoryResult>>.SuccessResponse(result.Data!, "Success"));
    }
}
