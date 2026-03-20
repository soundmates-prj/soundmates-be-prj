using AuthService.Api.Models.Requests.Spotify;
using AuthService.Api.Models.Responses;
using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.Features.SpotifyItems.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

/// <summary>
/// API for managing cached <c>SpotifyItem</c> metadata.
/// </summary>
[ApiController]
[Route("api/v1/spotify-items")]
[Authorize]
public class SpotifyItemsController : ControllerBase
{
    private readonly ICommandDispatcher _commands;

    public SpotifyItemsController(ICommandDispatcher commands)
    {
        _commands = commands;
    }

    /// <summary>
    /// Creates or updates (upserts) a Spotify item in cache.
    /// </summary>
    /// <remarks>
    /// The handler validates domain rules (item type enum, field lengths, etc.) before persisting.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSpotifyItemRequest request, CancellationToken ct)
    {
        // Validate request model at API layer
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<Guid>.FailureResponse("Invalid input", 400));

        var command = new CreateSpotifyItemCommand
        {
            SpotifyId = request.SpotifyId,
            ItemType = request.ItemType,
            Name = request.Name,
            ArtistName = request.ArtistName,
            AlbumName = request.AlbumName,
            ImgUrl = request.ImgUrl,
            PreviewUrl = request.PreviewUrl,
            RawJson = request.RawJson
        };

        var result = await _commands.Send<CreateSpotifyItemCommand, Guid>(command, ct);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<Guid>.FailureResponse(result.ErrorMessage ?? "Create spotify item failed", result.ErrorCode ?? 400));

        return StatusCode(StatusCodes.Status201Created, ApiResponse<Guid>.SuccessResponse(result.Data, result.ErrorMessage ?? "Create spotify item successful"));
    }

    /// <summary>
    /// Deletes a Spotify item from cache.
    /// </summary>
    /// <remarks>
    /// Deletion is blocked when the item is still referenced by user favourites.
    /// </remarks>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromBody] DeleteSpotifyItemRequest request, CancellationToken ct)
    {
        // Validate request model
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<bool>.FailureResponse("Invalid input", 400));

        var command = new DeleteSpotifyItemCommand
        {
            SpotifyId = request.SpotifyId,
            ItemType = request.ItemType
        };

        var result = await _commands.Send<DeleteSpotifyItemCommand, bool>(command, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                404 => NotFound(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Spotify item not found", 404)),
                409 => Conflict(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Spotify item is in use", 409)),
                _ => BadRequest(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Delete spotify item failed", result.ErrorCode ?? 400))
            };
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, result.ErrorMessage ?? "Spotify item deleted"));
    }
}
