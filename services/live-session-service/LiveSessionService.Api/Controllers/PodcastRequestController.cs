using LiveSessionService.Api.Extensions;
using LiveSessionService.Api.Models.Requests.LiveSessions;
using LiveSessionService.Api.Models.Responses;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.PodcastRequests.Commands.CancelPodcastRequest;
using LiveSessionService.Application.Features.PodcastRequests.Commands.CreatePodcastRequest;
using LiveSessionService.Application.Features.PodcastRequests.Commands.ReviewPodcastRequest;
using LiveSessionService.Application.Features.PodcastRequests.Queries.GetMyPodcastRequests;
using LiveSessionService.Application.Features.PodcastRequests.Queries.GetPodcastRequests;
using LiveSessionService.Application.Features.PodcastRequests.Queries.GetPodcastRequestById;
using LiveSessionService.Application.Features.Results.PodcastRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveSessionService.Api.Controllers;

[ApiController]
[Route("api/v1/podcast-requests")]
[Produces("application/json")]
[Authorize]
public sealed class PodcastRequestController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;
    private readonly ILiveSessionNotifier _notifier;

    public PodcastRequestController(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        ILiveSessionNotifier notifier)
    {
        _commands = commands;
        _queries = queries;
        _notifier = notifier;
    }

    /// <summary>
    /// User submits a new podcast request for a live session.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PodcastRequestResult>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePodcastRequestRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.FailureResponse(
                "Invalid or missing user token",
                (int)ErrorCode.Unauthorized));
        }

        var command = new CreatePodcastRequestCommand(
            RequestedByUserId: userId.Value,
            LiveSessionId: request.LiveSessionId,
            Title: request.Title,
            Description: request.Description,
            ScriptText: request.ScriptText,
            AudioUrl: request.AudioUrl,
            DurationSeconds: request.DurationSeconds,
            VoiceCode: request.VoiceCode,
            VoiceDisplayName: request.VoiceDisplayName);

        var result = await _commands.Send<CreatePodcastRequestCommand, PodcastRequestResult>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        // Broadcast to SignalR group
        _ = _notifier.NotifyPodcastRequestCreated(result.Data!, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Data!.Id },
            result.ToApiResponse());
    }

    /// <summary>
    /// Get all podcast requests (Staff/Admin). Supports filtering by session and status.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "STAFF,ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<List<PodcastRequestResult>>), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? sessionId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var query = new GetPodcastRequestsQuery(sessionId, status, search);
        var result = await _queries.Send<GetPodcastRequestsQuery, List<PodcastRequestResult>>(query, ct);

        if (!result.IsSuccess)
            return StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse());

        return Ok(ApiResponse<List<PodcastRequestResult>>.SuccessResponse(
            result.Data!,
            $"Retrieved {result.Data!.Count} request(s)"));
    }

    /// <summary>
    /// Get podcast requests made by the current user.
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<List<PodcastRequestResult>>), 200)]
    public async Task<IActionResult> GetMine(
        [FromQuery] Guid? sessionId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", (int)ErrorCode.Unauthorized));

        var query = new GetMyPodcastRequestsQuery(userId.Value, sessionId, status);
        var result = await _queries.Send<GetMyPodcastRequestsQuery, List<PodcastRequestResult>>(query, ct);

        return result.IsSuccess
            ? Ok(ApiResponse<List<PodcastRequestResult>>.SuccessResponse(result.Data!))
            : StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse());
    }

    /// <summary>
    /// Get a specific podcast request by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PodcastRequestResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _queries.Send<GetPodcastRequestByIdQuery, PodcastRequestResult>(
            new GetPodcastRequestByIdQuery(id), ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Staff reviews (approve/reject) a podcast request.
    /// On approval: audio is downloaded, converted, and uploaded to AzuraCast.
    /// </summary>
    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = "STAFF,ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<PodcastRequestResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Review(
        Guid id,
        [FromBody] ReviewPodcastRequestRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.FailureResponse("Invalid input", (int)ErrorCode.BadRequest));

        var reviewerId = GetCurrentUserId();
        if (reviewerId == null)
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", (int)ErrorCode.Unauthorized));

        if (string.IsNullOrWhiteSpace(request.Action))
            return BadRequest(ApiResponse<object>.FailureResponse("Action is required", (int)ErrorCode.BadRequest));

        var normalizedAction = request.Action.Trim().ToLowerInvariant();
        if (normalizedAction != "approve" && normalizedAction != "reject")
            return BadRequest(ApiResponse<object>.FailureResponse("Action must be 'approve' or 'reject'", (int)ErrorCode.BadRequest));

        var command = new ReviewPodcastRequestCommand(
            PodcastRequestId: id,
            ReviewedByUserId: reviewerId.Value,
            IsApproved: normalizedAction == "approve",
            RejectReason: normalizedAction == "reject" ? request.RejectReason : null);

        var result = await _commands.Send<ReviewPodcastRequestCommand, PodcastRequestResult>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                ErrorCode.Forbidden => StatusCode(403, result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        // Broadcast review result to SignalR group
        _ = _notifier.NotifyPodcastRequestReviewed(result.Data!, ct);

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// User cancels their own pending podcast request.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid token", (int)ErrorCode.Unauthorized));

        var result = await _commands.Send<CancelPodcastRequestCommand, bool>(
            new CancelPodcastRequestCommand(id, userId.Value), ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.Forbidden => StatusCode(403, result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(result.ToApiResponse());
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub")
            ?? User.FindFirst("user_id");

        if (claim != null && Guid.TryParse(claim.Value, out var id))
            return id;

        return null;
    }
}