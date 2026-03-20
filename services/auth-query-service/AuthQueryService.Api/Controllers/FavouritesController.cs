using AuthQueryService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AuthQueryService.Application.DTOs;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Application.Services.Favourites.Queries.GetMyFavourites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthQueryService.Api.Controllers
{
    /// <summary>
    /// Read-only endpoints for user favourites.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public class FavouritesController : ControllerBase
    {
        private readonly IQueryDispatcher _queries;
        private readonly ILogger<FavouritesController> _logger;

        public FavouritesController(IQueryDispatcher queries, ILogger<FavouritesController> logger)
        {
            _queries = queries;
            _logger = logger;
        }

        // ─────────────────────────────────────────
        //  My Favourites (authenticated)
        // ─────────────────────────────────────────

        /// <summary>
        /// Get the authenticated user's favourites.
        /// Optionally filter by itemType (track, artist, album, playlist, schedule…)
        /// and/or source (spotify, local).
        /// </summary>
        /// <param name="itemType">Optional filter: track | artist | album | playlist | podcast | episode | show | blog | schedule</param>
        /// <param name="source">Optional filter: spotify | local | other</param>
        [Authorize]
        [HttpGet("api/v1/me/favorites")]
        [ProducesResponseType(typeof(ApiResponse<List<UserFavouriteDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyFavourites(
            [FromQuery] string? itemType = null,
            [FromQuery] string? source = null,
            CancellationToken ct = default)
        {
            var userId = ResolveUserId();
            if (userId is null)
                return Unauthorized(ApiResponse<object>.FailureResponse("Invalid or missing user token.", 401));

            _logger.LogInformation("GetMyFavourites for user {UserId}, type={Type}, source={Source}",
                userId, itemType ?? "all", source ?? "all");

            var query = new GetMyFavouritesQuery(userId.Value, itemType, source);
            var result = await _queries.Query(query, ct);

            return result.Success ? Ok(result) : StatusCode(result.ErrorCode ?? 500, result);
        }

        // ─────────────────────────────────────────
        //  Public profile favourites (any user)
        // ─────────────────────────────────────────

        /// <summary>
        /// Get favourites for any user by their ID.
        /// Returns only items owned by that user (no cross-user leakage).
        /// </summary>
        [HttpGet("api/v1/users/{userId:guid}/favorites")]
        [ProducesResponseType(typeof(ApiResponse<List<UserFavouriteDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserFavourites(
            Guid userId,
            [FromQuery] string? itemType = null,
            [FromQuery] string? source = null,
            CancellationToken ct = default)
        {
            _logger.LogInformation("GetUserFavourites for user {UserId}, type={Type}, source={Source}",
                userId, itemType ?? "all", source ?? "all");

            var query = new GetMyFavouritesQuery(userId, itemType, source);
            var result = await _queries.Query(query, ct);

            return result.Success ? Ok(result) : StatusCode(result.ErrorCode ?? 500, result);
        }

        // ─────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────

        private Guid? ResolveUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("sub")
                     ?? User.Claims.FirstOrDefault(c => c.Type == "user_id");

            return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}
