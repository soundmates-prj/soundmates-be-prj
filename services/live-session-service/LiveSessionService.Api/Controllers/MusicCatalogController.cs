using Microsoft.AspNetCore.Mvc;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results.Music;
using LiveSessionService.Application.Features.Music.Commands.UploadMusic;
using LiveSessionService.Api.Models.Responses;
using LiveSessionService.Api.Models.Requests.Music;
using LiveSessionService.Api.Extensions;

namespace LiveSessionService.Api.Controllers;

/// <summary>
/// API endpoints for Music Catalog management
/// Handles music file uploads and organization
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
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

        _logger.LogWarning("UploadMusic handler not yet implemented");
        
        return StatusCode(501, ApiResponse<object>.FailureResponse(
            "Music upload feature coming soon",
            501));
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
