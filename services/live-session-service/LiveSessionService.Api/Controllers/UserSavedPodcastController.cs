using LiveSessionService.Api.Extensions;
using LiveSessionService.Api.Models.Responses;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Podcasts.Commands.FollowPodcast;
using LiveSessionService.Application.Features.Podcasts.Commands.UnfollowPodcast;
using LiveSessionService.Application.Features.Podcasts.Queries.GetFollowedPodcasts;
using LiveSessionService.Application.Features.Results.Podcasts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LiveSessionService.Api.Controllers;

/// <summary>
/// API endpoints for managing User's saved podcasts
/// </summary>
[ApiController]
[Route("api/v1/me/saved-podcasts")]
[Produces("application/json")]
[Authorize]
public sealed class UserSavedPodcastController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public UserSavedPodcastController(ICommandDispatcher commands, IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    /// <summary>
    /// Retrieves the list of podcasts followed by the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PodcastResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse(
                "Invalid or missing user token",
                (int)ErrorCode.Unauthorized));
        }

        var result = await _queries.Send<GetFollowedPodcastsQuery, List<PodcastResult>>(
            new GetFollowedPodcastsQuery(userId),
            ct);

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Follows a podcast for the current user
    /// </summary>
    [HttpPost("{podcastId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PodcastResult>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> Follow(Guid podcastId, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse(
                "Invalid or missing user token",
                (int)ErrorCode.Unauthorized));
        }

        var result = await _commands.Send<FollowPodcastCommand, PodcastResult>(
            new FollowPodcastCommand(userId, podcastId),
            ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.Conflict => Conflict(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return StatusCode(201, result.ToApiResponse());
    }

    /// <summary>
    /// Unfollows a podcast for the current user 
    /// </summary>
    [HttpDelete("{podcastId:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Unfollow(Guid podcastId, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse(
                "Invalid or missing user token",
                (int)ErrorCode.Unauthorized));
        }

        var result = await _commands.Send(new UnfollowPodcastCommand(userId, podcastId), ct);

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
