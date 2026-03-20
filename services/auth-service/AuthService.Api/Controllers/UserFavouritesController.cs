using AuthService.Api.Models.Requests.User;
using AuthService.Api.Models.Responses;
using AuthService.Api.Extensions;
using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.Features.Users.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

/// <summary>
/// User Favourites API
/// Handles adding, updating, and removing favourites (Write-side).
/// Automatically enriches metadata from Spotify when applicable.
/// All changes are dual-written to MongoDB for read queries.
/// </summary>
[ApiController]
[Route("api/v1/me/favorites")]
[Authorize]
public class UserFavouritesController : ControllerBase
{
    private readonly ICommandDispatcher _commands;

    public UserFavouritesController(ICommandDispatcher commands)
    {
        _commands = commands;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  POST /api/v1/me/favorites
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Add an item to the current user's favourites.
    /// When source = "spotify", Spotify metadata is auto-fetched by itemId — you do NOT
    /// need to supply Name/ArtistName/etc.; they are enriched server-side from Spotify API.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserFavouriteRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<Guid>.FailureResponse("Invalid input", 400));

        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<Guid>.FailureResponse("Invalid or missing user token", 401));

        var command = new CreateUserFavouriteCommand
        {
            UserId     = userId,
            ItemType   = request.ItemType,
            ItemId     = request.ItemId,
            Source     = request.Source,
            Name       = request.Name,
            ArtistName = request.ArtistName,
            AlbumName  = request.AlbumName,
            ImgUrl     = request.ImgUrl,
            PreviewUrl = request.PreviewUrl,
            RawJson    = request.RawJson
        };

        var result = await _commands.Send<CreateUserFavouriteCommand, Guid>(command, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                404 => NotFound(ApiResponse<Guid>.FailureResponse(result.ErrorMessage ?? "User not found", 404)),
                409 => Conflict(ApiResponse<Guid>.FailureResponse(result.ErrorMessage ?? "Favourite already exists", 409)),
                _ => BadRequest(ApiResponse<Guid>.FailureResponse(result.ErrorMessage ?? "Create favourite failed", result.ErrorCode ?? 400))
            };
        }

        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<Guid>.SuccessResponse(result.Data, "Added to favourites"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PUT /api/v1/me/favorites
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Update metadata of a favourite item.
    ///
    /// Two modes:
    /// - refreshFromSpotify = false (default) → partial patch; only provided fields are updated.
    /// - refreshFromSpotify = true            → ignores body metadata, re-fetches from Spotify API.
    ///
    /// The business key (itemType + itemId + source) identifies which favourite to update.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateUserFavouriteRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<bool>.FailureResponse("Invalid input", 400));

        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid or missing user token", 401));

        var command = new UpdateUserFavouriteCommand
        {
            UserId             = userId,
            ItemType           = request.ItemType,
            ItemId             = request.ItemId,
            Source             = request.Source,
            Name               = request.Name,
            ArtistName         = request.ArtistName,
            AlbumName          = request.AlbumName,
            ImgUrl             = request.ImgUrl,
            PreviewUrl         = request.PreviewUrl,
            RefreshFromSpotify = request.RefreshFromSpotify
        };

        var result = await _commands.Send<UpdateUserFavouriteCommand, bool>(command, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                404 => NotFound(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Favourite not found", 404)),
                _ => BadRequest(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Update failed", result.ErrorCode ?? 400))
            };
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, result.ErrorMessage ?? "Favourite updated"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  DELETE /api/v1/me/favorites
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Remove an item from the current user's favourites.
    /// If the item is a Spotify item with no remaining references, the cached metadata is also removed.
    /// Data is also removed from the MongoDB read-side (dual-write).
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromBody] DeleteUserFavouriteRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<bool>.FailureResponse("Invalid input", 400));

        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid or missing user token", 401));

        var command = new DeleteUserFavouriteCommand
        {
            UserId   = userId,
            ItemType = request.ItemType,
            ItemId   = request.ItemId,
            Source   = request.Source
        };

        var result = await _commands.Send<DeleteUserFavouriteCommand, bool>(command, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                404 => NotFound(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Favourite not found", 404)),
                _ => BadRequest(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Delete failed", result.ErrorCode ?? 400))
            };
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Removed from favourites"));
    }
}
