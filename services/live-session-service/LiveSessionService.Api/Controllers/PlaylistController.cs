using Microsoft.AspNetCore.Mvc;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results.Playlists;
using LiveSessionService.Application.Features.Playlists.Commands.CreatePlaylist;
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

        // implementations here....

        return StatusCode(501, ApiResponse<object>.FailureResponse(
            "Playlist creation feature coming soon",
            501));
    }

    /// <summary>
    /// Get all playlists for a station
    /// </summary>
    [HttpGet("station/{stationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<PlaylistResult>>), 200)]
    public async Task<IActionResult> GetStationPlaylists(Guid stationId, CancellationToken ct)
    {
        _logger.LogWarning("GetStationPlaylists not yet implemented");

        return Ok(ApiResponse<List<PlaylistResult>>.SuccessResponse(
            new List<PlaylistResult>(),
            "Feature coming soon"));
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
        _logger.LogWarning("AddTracksToPlaylist not yet implemented");

        return StatusCode(501, ApiResponse<object>.FailureResponse(
            "Add tracks feature coming soon",
            501));
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
