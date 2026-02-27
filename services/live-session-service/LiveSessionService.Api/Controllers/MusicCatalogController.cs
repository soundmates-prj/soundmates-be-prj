using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results.Music;
using LiveSessionService.Application.Features.Music.Commands.UploadMusic;
using LiveSessionService.Api.Models.Responses;
using LiveSessionService.Api.Models.Requests.Music;
using LiveSessionService.Api.Extensions;
using LiveSessionService.Api.Helpers;
using System.Security.Claims;

namespace LiveSessionService.Api.Controllers;

/// <summary>
/// API endpoints for Music Catalog management
/// Handles music file uploads and organization
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class MusicCatalogController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;
    private readonly ILogger<MusicCatalogController> _logger;

    public MusicCatalogController(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        ILogger<MusicCatalogController> _logger)
    {
        _commands = commands;
        _queries = queries;
        this._logger = _logger;
    }

    /// <summary>
    /// Upload a music file to station
    /// </summary>
    /// <remarks>
    /// Supported formats: MP3, FLAC, WAV, OGG
    /// Max file size: 100MB
    /// </remarks>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100_000_000)] // 100MB
    [ProducesResponseType(typeof(ApiResponse<MusicResult>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> UploadMusic(
        [FromForm] UploadMusicRequest request,
        CancellationToken ct)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "No file uploaded",
                (int)ErrorCode.BadRequest));
        }

        // Validate file type
        var allowedExtensions = new[] { ".mp3", ".flac", ".wav", ".ogg" };
        var extension = Path.GetExtension(request.File.FileName).ToLower();

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                $"Unsupported file type. Allowed: {string.Join(", ", allowedExtensions)}",
                (int)ErrorCode.BadRequest));
        }

        // Copy to MemoryStream — IFormFile stream is not guaranteed to be seekable
        var ms = new MemoryStream();
        await request.File.CopyToAsync(ms, ct);
        ms.Position = 0;

        // Auto-extract ID3/Vorbis tags from file so users don't have to fill them manually
        string? tagTitle = null, tagArtist = null, tagAlbum = null;
        try
        {
            var abstraction = new TagLibStreamAbstraction(ms, request.File.FileName);
            using var tagFile = TagLib.File.Create(abstraction);
            tagTitle  = string.IsNullOrWhiteSpace(tagFile.Tag.Title)  ? null : tagFile.Tag.Title.Trim();
            tagArtist = tagFile.Tag.Performers?.Length > 0
                ? string.Join(", ", tagFile.Tag.Performers).Trim()
                : null;
            tagAlbum  = string.IsNullOrWhiteSpace(tagFile.Tag.Album)  ? null : tagFile.Tag.Album.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read tags from {FileName} — using request values or fallbacks",
                request.File.FileName);
        }

        ms.Position = 0;

        // Priority: request field > file tag > fallback
        var title  = (!string.IsNullOrWhiteSpace(request.Title)  ? request.Title  : tagTitle)
                     ?? Path.GetFileNameWithoutExtension(request.File.FileName);
        var artist = (!string.IsNullOrWhiteSpace(request.Artist) ? request.Artist : tagArtist)
                     ?? "Unknown Artist";
        var album  = !string.IsNullOrWhiteSpace(request.Album)   ? request.Album  : tagAlbum;

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim missing from token"));

        var result = await _commands.Send<UploadMusicCommand, MusicResult>(
            new UploadMusicCommand(request.StationId, userId, title, artist, album,
                ms, request.File.FileName, request.File.ContentType), ct);

        await ms.DisposeAsync();

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.Unauthorized => Unauthorized(result.ToApiResponse()),
                ErrorCode.Forbidden => StatusCode(403, result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                ErrorCode.UnprocessableEntity => StatusCode(422, result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return StatusCode(201, result.ToApiResponse());
    }

    /// <summary>
    /// Get all music for a station
    /// </summary>
    [HttpGet("station/{stationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<MusicResult>>), 200)]
    public async Task<IActionResult> GetStationMusic(Guid stationId, CancellationToken ct)
    {
        _logger.LogWarning("GetStationMusic not yet implemented");
        
        return Ok(ApiResponse<List<MusicResult>>.SuccessResponse(
            new List<MusicResult>(),
            "Feature coming soon"));
    }

    /// <summary>
    /// Delete a music file
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteMusic(Guid id, CancellationToken ct)
    {
        _logger.LogWarning("DeleteMusic not yet implemented");
        
        return StatusCode(501, ApiResponse<object>.FailureResponse(
            "Delete music feature coming soon",
            501));
    }
}
