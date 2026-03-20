using AuthQueryService.Application.Abstractions;
using AuthQueryService.Application.DTOs.Response;
using AuthQueryService.Application.DTOs.Spotify;
using AuthQueryService.Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Mvc;

namespace AuthQueryService.Api.Controllers
{
    /// <summary>
    /// Proxy endpoints for Spotify Web API.
    /// </summary>
    [ApiController]
    [Route("api/v1/spotify")]
    [Produces("application/json")]
    public class SpotifyController : ControllerBase
    {
        private readonly ISpotifyApiClient _spotify;
        private readonly ILogger<SpotifyController> _logger;

        public SpotifyController(ISpotifyApiClient spotify, ILogger<SpotifyController> logger)
        {
            _spotify = spotify;
            _logger = logger;
        }

        // ─────────────────────────────────────────
        //  Search
        // ─────────────────────────────────────────

        /// <summary>
        /// Search Spotify for tracks, artists, and/or albums.
        /// </summary>
        /// <param name="q">Free-text search query (required)</param>
        /// <param name="type">Comma-separated types: track, artist, album (default: track)</param>
        /// <param name="limit">1-50, default 20</param>
        /// <param name="offset">Pagination offset, default 0</param>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<SpotifySearchResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Search(
            [FromQuery] string q,
            [FromQuery] string type = "track",
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(ApiResponse<object>.FailureResponse("Query parameter 'q' is required.", 400));

            var userAccessToken = Request.Headers["X-Spotify-Access-Token"].FirstOrDefault();

            try
            {
                var result = await _spotify.SearchAsync(q, type, limit, offset, ct, userAccessToken);
                return Ok(ApiResponse<SpotifySearchResult>.SuccessResponse(result, "Search completed."));
            }
            catch (SpotifyRateLimitException ex)
            {
                _logger.LogWarning("Spotify rate limit. Retry after {S}s.", ex.RetryAfter.TotalSeconds);
                Response.Headers.Append("Retry-After", ((int)ex.RetryAfter.TotalSeconds).ToString());
                return StatusCode(429, ApiResponse<object>.FailureResponse(
                    $"Spotify rate limit. Retry after {ex.RetryAfter.TotalSeconds}s.", 429));
            }
            catch (SpotifyApiException ex)
            {
                _logger.LogError(ex, "Spotify search error. Body: {Body}", ex.ResponseBody);
                return SpotifyError(ex);
            }
        }

        // ─────────────────────────────────────────
        //  Single item endpoints
        // ─────────────────────────────────────────

        /// <summary>Get a Spotify track by ID.</summary>
        [HttpGet("tracks/{trackId}")]
        [ProducesResponseType(typeof(ApiResponse<SpotifyTrackDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTrack(string trackId, CancellationToken ct)
        {
            try
            {
                var track = await _spotify.GetTrackAsync(trackId, ct);
                return track is null
                    ? NotFound(ApiResponse<object>.FailureResponse($"Track '{trackId}' not found.", 404))
                    : Ok(ApiResponse<SpotifyTrackDto>.SuccessResponse(track));
            }
            catch (SpotifyRateLimitException ex)
            {
                return StatusCode(429, ApiResponse<object>.FailureResponse(ex.Message, 429));
            }
            catch (SpotifyApiException ex)
            {
                _logger.LogError(ex, "Spotify error for track {Id}. Body: {Body}", trackId, ex.ResponseBody);
                return SpotifyError(ex);
            }
        }

        /// <summary>Get a Spotify artist by ID.</summary>
        [HttpGet("artists/{artistId}")]
        [ProducesResponseType(typeof(ApiResponse<SpotifyArtistDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetArtist(string artistId, CancellationToken ct)
        {
            try
            {
                var artist = await _spotify.GetArtistAsync(artistId, ct);
                return artist is null
                    ? NotFound(ApiResponse<object>.FailureResponse($"Artist '{artistId}' not found.", 404))
                    : Ok(ApiResponse<SpotifyArtistDto>.SuccessResponse(artist));
            }
            catch (SpotifyRateLimitException ex)
            {
                return StatusCode(429, ApiResponse<object>.FailureResponse(ex.Message, 429));
            }
            catch (SpotifyApiException ex)
            {
                _logger.LogError(ex, "Spotify error for artist {Id}. Body: {Body}", artistId, ex.ResponseBody);
                return SpotifyError(ex);
            }
        }

        /// <summary>Get a Spotify album by ID.</summary>
        [HttpGet("albums/{albumId}")]
        [ProducesResponseType(typeof(ApiResponse<SpotifyAlbumDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAlbum(string albumId, CancellationToken ct)
        {
            try
            {
                var album = await _spotify.GetAlbumAsync(albumId, ct);
                return album is null
                    ? NotFound(ApiResponse<object>.FailureResponse($"Album '{albumId}' not found.", 404))
                    : Ok(ApiResponse<SpotifyAlbumDto>.SuccessResponse(album));
            }
            catch (SpotifyRateLimitException ex)
            {
                return StatusCode(429, ApiResponse<object>.FailureResponse(ex.Message, 429));
            }
            catch (SpotifyApiException ex)
            {
                _logger.LogError(ex, "Spotify error for album {Id}. Body: {Body}", albumId, ex.ResponseBody);
                return SpotifyError(ex);
            }
        }

        // ─────────────────────────────────────────
        //  Batch
        // ─────────────────────────────────────────

        /// <summary>Get multiple tracks by comma-separated Spotify IDs (max 50).</summary>
        [HttpGet("tracks")]
        [ProducesResponseType(typeof(ApiResponse<List<SpotifyTrackDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSeveralTracks([FromQuery] string ids, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return BadRequest(ApiResponse<object>.FailureResponse("'ids' query parameter is required.", 400));

            var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (idList.Length == 0)
                return BadRequest(ApiResponse<object>.FailureResponse("No valid IDs provided.", 400));

            try
            {
                var tracks = await _spotify.GetSeveralTracksAsync(idList, ct);
                return Ok(ApiResponse<List<SpotifyTrackDto>>.SuccessResponse(
                    tracks, $"Retrieved {tracks.Count} track(s)."));
            }
            catch (SpotifyRateLimitException ex)
            {
                return StatusCode(429, ApiResponse<object>.FailureResponse(ex.Message, 429));
            }
            catch (SpotifyApiException ex)
            {
                _logger.LogError(ex, "Spotify batch tracks error. Body: {Body}", ex.ResponseBody);
                return SpotifyError(ex);
            }
        }

        // ─────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────

        /// <summary>
        /// Maps a Spotify API exception to the appropriate HTTP status code.
        /// 4xx from Spotify = client error (bad input) → forward same code back.
        /// 5xx from Spotify = upstream failure → return 503.
        /// </summary>
        private IActionResult SpotifyError(SpotifyApiException ex)
        {
            if (ex.StatusCode >= 400 && ex.StatusCode < 500)
            {
                // Spotify rejected the request parameter (bad query, bad ID, etc.)
                // Surface the real message to the caller so they can fix the request.
                return StatusCode(ex.StatusCode,
                    ApiResponse<object>.FailureResponse(
                        $"Spotify error: {ex.ResponseBody}", ex.StatusCode));
            }
            // Spotify server error — don't leak internals
            return StatusCode(503, ApiResponse<object>.FailureResponse("Spotify service temporarily unavailable.", 503));
        }
    }
}
