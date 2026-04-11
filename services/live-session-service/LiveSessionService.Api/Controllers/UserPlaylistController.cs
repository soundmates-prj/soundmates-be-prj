using LiveSessionService.Api.Extensions;
using LiveSessionService.Api.Models.Requests.Playlist;
using LiveSessionService.Api.Models.Responses;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Playlists.Commands.AddTracksToUserPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.CreateUserPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.DeleteUserPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.RemoveTracksFromUserPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.UpdateUserPlaylist;
using LiveSessionService.Application.Features.Playlists.Queries.GetUserPlaylistById;
using LiveSessionService.Application.Features.Playlists.Queries.GetUserPlaylistTracks;
using LiveSessionService.Application.Features.Playlists.Queries.GetUserPlaylists;
using LiveSessionService.Application.Features.Results.Playlists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LiveSessionService.Api.Controllers;

/// <summary>
/// API endpoints for User Playlist management
/// User Playlists are personalized playlists created and managed by individual users
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public sealed class UserPlaylistController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public UserPlaylistController(ICommandDispatcher commands, IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    /// <summary>
    /// Get all playlists for the current user
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<UserPlaylistResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid or missing user token", (int)ErrorCode.Unauthorized));
        }

        var result = await _queries.Send<GetUserPlaylistsQuery, List<UserPlaylistResult>>(
            new GetUserPlaylistsQuery(userId),
            ct);

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Get all public user playlists
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<UserPlaylistResult>>), 200)]
    public async Task<IActionResult> GetAllPublic(CancellationToken ct)
    {
        var result = await _queries.Send<LiveSessionService.Application.Features.Playlists.Queries.GetAllPublicUserPlaylists.GetAllPublicUserPlaylistsQuery, List<UserPlaylistResult>>(
            new LiveSessionService.Application.Features.Playlists.Queries.GetAllPublicUserPlaylists.GetAllPublicUserPlaylistsQuery(),
            ct);

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Get a specific playlist by ID for the current user
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserPlaylistResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid or missing user token", (int)ErrorCode.Unauthorized));
        }

        var result = await _queries.Send<GetUserPlaylistByIdQuery, UserPlaylistResult>(
            new GetUserPlaylistByIdQuery(id, userId),
            ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.Forbidden => StatusCode(403, result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Create a new user playlist for the current user
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserPlaylistResult>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Create([FromBody] CreateUserPlaylistRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse("Invalid input", (int)ErrorCode.BadRequest));
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid or missing user token", (int)ErrorCode.Unauthorized));
        }

        var command = new CreateUserPlaylistCommand(
            userId,
            request.PlaylistName,
            request.Description,
            request.ThumbnailUrl,
            request.Visibility,
            request.IsEnabled);

        var result = await _commands.Send<CreateUserPlaylistCommand, UserPlaylistResult>(command, ct);

        if (!result.IsSuccess)
        {
            return StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse());
        }

        return StatusCode(201, result.ToApiResponse());
    }

    /// <summary>
    /// Update an existing user playlist for the current user
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserPlaylistResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserPlaylistRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse("Invalid input", (int)ErrorCode.BadRequest));
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid or missing user token", (int)ErrorCode.Unauthorized));
        }

        var command = new UpdateUserPlaylistCommand(
            id,
            userId,
            request.PlaylistName,
            request.Description,
            request.ThumbnailUrl,
            request.Visibility,
            request.IsEnabled);

        var result = await _commands.Send<UpdateUserPlaylistCommand, UserPlaylistResult>(command, ct);

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

    /// <summary>
    /// Delete a user playlist for the current user
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid or missing user token", (int)ErrorCode.Unauthorized));
        }

        var result = await _commands.Send(new DeleteUserPlaylistCommand(id, userId), ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.Forbidden => StatusCode(403, result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Get all tracks of a user playlist for the current user
    /// </summary>
    [HttpGet("{id:guid}/tracks")]
    [ProducesResponseType(typeof(ApiResponse<List<PlaylistMediaResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetTracks(Guid id, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid or missing user token", (int)ErrorCode.Unauthorized));
        }

        var result = await _queries.Send<GetUserPlaylistTracksQuery, List<PlaylistMediaResult>>(
            new GetUserPlaylistTracksQuery(id, userId),
            ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.Forbidden => StatusCode(403, result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Add tracks to a user playlist for the current user
    /// </summary>
    [HttpPost("{id:guid}/tracks")]
    [ProducesResponseType(typeof(ApiResponse<List<PlaylistMediaResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> AddTracks(Guid id, [FromBody] AddUserPlaylistTracksRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse("Invalid input", (int)ErrorCode.BadRequest));
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid or missing user token", (int)ErrorCode.Unauthorized));
        }

        var result = await _commands.Send<AddTracksToUserPlaylistCommand, List<PlaylistMediaResult>>(
            new AddTracksToUserPlaylistCommand(id, userId, request.MediaIds),
            ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.Forbidden => StatusCode(403, result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Remove tracks from a user playlist for the current user
    /// </summary>
    [HttpDelete("{id:guid}/tracks")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> RemoveTracks(Guid id, [FromBody] RemoveUserPlaylistTracksRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse("Invalid input", (int)ErrorCode.BadRequest));
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse("Invalid or missing user token", (int)ErrorCode.Unauthorized));
        }

        var result = await _commands.Send(
            new RemoveTracksFromUserPlaylistCommand(id, userId, request.MediaIds),
            ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.Forbidden => StatusCode(403, result.ToApiResponse()),
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
