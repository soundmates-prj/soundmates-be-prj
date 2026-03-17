using AuthService.Api.Models.Requests.User;
using AuthService.Api.Models.Responses;
using AuthService.Api.Extensions;
using AuthService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthService.Application.Features.Users.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

/// <summary>
/// API quản lý danh sách yêu thích của người dùng đăng nhập hiện tại.
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

    /// <summary>
    /// Adds an item to the current user's favourites.
    /// If <c>source = spotify</c>, attached Spotify metadata is cached as well.
    /// </summary>
    /// <remarks>
    /// Processing flow:
    /// 1) Validate request model.
    /// 2) Resolve <c>UserId</c> from JWT claims.
    /// 3) Dispatch command to Application handler.
    /// 4) Map <c>Result</c> to the appropriate HTTP status code.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserFavouriteRequest request, CancellationToken ct)
    {
        // Validate request model at API layer (DataAnnotations)
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<Guid>.FailureResponse("Invalid input", 400));

        // UserId is always resolved from token to prevent payload spoofing
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<Guid>.FailureResponse("Invalid or missing user token", 401));

        var command = new CreateUserFavouriteCommand
        {
            UserId = userId,
            ItemType = request.ItemType,
            ItemId = request.ItemId,
            Source = request.Source,
            Name = request.Name,
            ArtistName = request.ArtistName,
            AlbumName = request.AlbumName,
            ImgUrl = request.ImgUrl,
            PreviewUrl = request.PreviewUrl,
            RawJson = request.RawJson
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

        return StatusCode(StatusCodes.Status201Created, ApiResponse<Guid>.SuccessResponse(result.Data, result.ErrorMessage ?? "Create user favourite successful"));
    }

    /// <summary>
    /// Removes an item from the current user's favourites.
    /// </summary>
    /// <remarks>
    /// If the item source is Spotify and no references remain,
    /// the related cached Spotify metadata may also be cleaned up.
    /// </remarks>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromBody] DeleteUserFavouriteRequest request, CancellationToken ct)
    {
        // Validate request model
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<bool>.FailureResponse("Invalid input", 400));

        // Resolve current user from JWT claims
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<bool>.FailureResponse("Invalid or missing user token", 401));

        var command = new DeleteUserFavouriteCommand
        {
            UserId = userId,
            ItemType = request.ItemType,
            ItemId = request.ItemId,
            Source = request.Source
        };

        var result = await _commands.Send<DeleteUserFavouriteCommand, bool>(command, ct);
        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                404 => NotFound(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Favourite not found", 404)),
                _ => BadRequest(ApiResponse<bool>.FailureResponse(result.ErrorMessage ?? "Delete favourite failed", result.ErrorCode ?? 400))
            };
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, result.ErrorMessage ?? "Removed from favourites"));
    }

}
