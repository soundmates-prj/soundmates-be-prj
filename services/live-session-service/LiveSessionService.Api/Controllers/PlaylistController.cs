using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Application.Features.Playlists.Commands.CreatePlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.AddMediaToPlaylist;
using LiveSessionService.Application.Features.Playlists.Commands.SyncPlaylists;
using LiveSessionService.Application.Features.Playlists.Queries.GetPlaylistsByStation;
using LiveSessionService.Api.Models.Responses;
using LiveSessionService.Api.Extensions;
using LiveSessionService.Api.Models.Requests.Playlist;

namespace LiveSessionService.Api.Controllers;

/// <summary>
/// API endpoints for Playlist management
/// Playlists organize music for live streaming sessions
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class PlaylistController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;
    private readonly ILogger<PlaylistController> _logger;

    public PlaylistController(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        ILogger<PlaylistController> logger)
    {
        _commands = commands;
        _queries = queries;
        _logger = logger;
    }

    /// <summary>
    /// Create a new playlist
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PlaylistResult>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CreatePlaylist(
        [FromBody] CreatePlaylistRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        var result = await _commands.Send<CreatePlaylistCommand, PlaylistResult>(
            new CreatePlaylistCommand(request.StationId, request.PlaylistName, request.Description, request.IsAutoPlay), ct);

        if (!result.IsSuccess)
            return BadRequest(result.ToApiResponse());

        return StatusCode(201, result.ToApiResponse());
    }

    /// <summary>
    /// Sync playlists from AzuraCast for a specific station
    /// </summary>
    /// <param name="stationId">Station ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Sync result with playlist information</returns>
    [HttpPost("station/{stationId:guid}/sync")]
    [ProducesResponseType(typeof(ApiResponse<SyncPlaylistsResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> SyncStationPlaylists(Guid stationId, CancellationToken ct)
    {
        var result = await _commands.Send<SyncPlaylistsCommand, SyncPlaylistsResult>(
            new SyncPlaylistsCommand(stationId), ct);

        if (!result.IsSuccess)
            return result.ErrorCode == ErrorCode.NotFound
                ? NotFound(result.ToApiResponse())
                : StatusCode(500, result.ToApiResponse());

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Get all playlists for a station
    /// </summary>
    [HttpGet("station/{stationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<PlaylistResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetStationPlaylists(Guid stationId, CancellationToken ct)
    {
        var result = await _queries.Send<GetPlaylistsByStationQuery, List<PlaylistResult>>(
            new GetPlaylistsByStationQuery(stationId), ct);

        if (!result.IsSuccess)
            return result.ErrorCode == ErrorCode.NotFound 
                ? NotFound(result.ToApiResponse()) 
                : BadRequest(result.ToApiResponse());

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Add music tracks to playlist
    /// </summary>
    [HttpPost("{playlistId:guid}/tracks")]
    [ProducesResponseType(typeof(ApiResponse<PlaylistResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> AddTracksToPlaylist(
        Guid playlistId,
        [FromBody] AddTracksRequest request,
        CancellationToken ct)
    {
        var results = new List<PlaylistMediaResult>();

        foreach (var musicId in request.MusicIds)
        {
            var r = await _commands.Send<AddMediaToPlaylistCommand, PlaylistMediaResult>(new AddMediaToPlaylistCommand(playlistId, musicId), ct);
            if (!r.IsSuccess)
                return BadRequest(r.ToApiResponse());
            results.Add(r.Data!);
        }

        return Ok(ApiResponse<List<PlaylistMediaResult>>.SuccessResponse(results, "Tracks added to playlist"));
    }

    /// <summary>
    /// Remove tracks from playlist
    /// </summary>
    [HttpDelete("{playlistId:guid}/tracks")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> RemoveTracksFromPlaylist(
        Guid playlistId,
        [FromBody] RemoveTracksRequest request,
        CancellationToken ct)
    {
        _logger.LogWarning("RemoveTracksFromPlaylist not yet implemented");

        return StatusCode(501, ApiResponse<object>.FailureResponse(
            "Remove tracks feature coming soon",
            501));
    }

    /// <summary>
    /// Delete a playlist
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeletePlaylist(Guid id, CancellationToken ct)
    {
        _logger.LogWarning("DeletePlaylist not yet implemented");

        return StatusCode(501, ApiResponse<object>.FailureResponse(
            "Delete playlist feature coming soon",
            501));
    }
}

public sealed class AddTracksRequest
{
    public List<Guid> MusicIds { get; set; } = new();
}

public sealed class RemoveTracksRequest
{
    public List<Guid> MusicIds { get; set; } = new();
}
