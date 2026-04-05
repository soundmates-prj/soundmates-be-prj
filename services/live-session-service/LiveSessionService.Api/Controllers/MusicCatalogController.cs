using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Results.Music;
using LiveSessionService.Application.Features.Music.Commands.SyncMediaFiles;
using LiveSessionService.Application.Features.Music.Commands.UploadMusic;
using LiveSessionService.Application.Features.Music.Commands.BulkUploadMusic;
using LiveSessionService.Application.Features.Music.Commands.ImportSystemMediaBatch;
using LiveSessionService.Application.Features.Music.Commands.DeleteMedia;
using LiveSessionService.Application.Features.Music.Queries.GetAllMediaFiles;
using LiveSessionService.Application.Features.Music.Queries.GetMediaFilesByStation;
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
    /// Get all media files in music catalog
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<MusicResult>>), 200)]
    public async Task<IActionResult> GetAllMusic(CancellationToken ct)
    {
        var result = await _queries.Send<GetAllMediaFilesQuery, List<MusicResult>>(
            new GetAllMediaFilesQuery(), ct);

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

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Upload a music file to system media catalog
    /// </summary>
    /// <remarks>
    /// Supported formats: MP3, FLAC, WAV, OGG
    /// Max file size: 100MB
    /// File is stored in standalone system media storage (Auto-pushed to AzuraCast station)
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

        // Copy to MemoryStream � IFormFile stream is not guaranteed to be seekable
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
            _logger.LogWarning(ex, "Could not read tags from {FileName} � using request values or fallbacks",
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
    /// Sync music files from AzuraCast for a station
    /// </summary>
    [HttpPost("station/{stationId:guid}/sync")]
    [ProducesResponseType(typeof(ApiResponse<SyncMediaFilesResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> SyncStationMusic(Guid stationId, CancellationToken ct)
    {
        var result = await _commands.Send<SyncMediaFilesCommand, SyncMediaFilesResult>(
            new SyncMediaFilesCommand(stationId), ct);

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

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Explicit batch import of system media files into station media library
    /// </summary>
    [HttpPost("station/{stationId:guid}/import-system-media")]
    [ProducesResponseType(typeof(ApiResponse<ImportSystemMediaBatchResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> ImportSystemMediaBatch(
        Guid stationId,
        [FromBody] ImportSystemMediaBatchRequest request,
        CancellationToken ct)
    {
        if (request.MediaFileIds.Count == 0)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "At least one media id is required",
                (int)ErrorCode.BadRequest));
        }

        var result = await _commands.Send<ImportSystemMediaBatchCommand, ImportSystemMediaBatchResult>(
            new ImportSystemMediaBatchCommand(stationId, request.MediaFileIds), ct);

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

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Get all music for a station
    /// </summary>
    [HttpGet("station/{stationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<MusicResult>>), 200)]
    public async Task<IActionResult> GetStationMusic(Guid stationId, CancellationToken ct)
    {
        _logger.LogInformation("GetStationMusic: API called with StationId={StationId}", stationId);
        
        var result = await _queries.Send<GetMediaFilesByStationQuery, List<MusicResult>>(
            new GetMediaFilesByStationQuery(stationId), ct);

        _logger.LogInformation("GetStationMusic: Query completed. IsSuccess={IsSuccess}, ResultCount={Count}, ErrorCode={ErrorCode}",
            result.IsSuccess, result.Data?.Count ?? 0, result.ErrorCode);

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

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Delete a music file
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteMusic(Guid id, CancellationToken ct)
    {
        var result = await _commands.Send(new DeleteMediaCommand(id), ct);

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

        return NoContent();
    }

    /// <summary>
    /// Bulk upload multiple music files at once
    /// </summary>
    /// <remarks>
    /// Supported formats: MP3, FLAC, WAV, OGG
    /// Max individual file size: 100MB
    /// Max total request size: 500MB
    /// Max files per request: 100
    /// Files are stored in standalone system media storage (not auto-pushed to AzuraCast station)
    /// </remarks>
    [HttpPost("bulk-upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(524_288_000)] // 500MB
    [ProducesResponseType(typeof(ApiResponse<BulkUploadMusicResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> BulkUploadMusic(
        [FromForm] BulkUploadMusicRequest request,
        CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim missing from token"));

        // Validate at least one file
        if (request.Files == null || request.Files.Count == 0)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "No files uploaded",
                (int)ErrorCode.BadRequest));
        }

        // Validate file count
        if (request.Files.Count > 100)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Maximum 100 files allowed per bulk upload request",
                (int)ErrorCode.BadRequest));
        }

        // Validate file types
        var allowedExtensions = new[] { ".mp3", ".flac", ".wav", ".ogg" };
        var invalidFiles = request.Files
            .Where(f => !allowedExtensions.Contains(Path.GetExtension(f.FileName).ToLowerInvariant()))
            .Select(f => f.FileName)
            .ToList();

        if (invalidFiles.Count > 0)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                $"Unsupported file types. Allowed: {string.Join(", ", allowedExtensions)}. Invalid files: {string.Join(", ", invalidFiles)}",
                (int)ErrorCode.BadRequest));
        }

        // Convert IFormFile to BulkUploadFileEntry
        var fileEntries = new List<BulkUploadFileEntry>();
        foreach (var file in request.Files)
        {
            var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            ms.Position = 0;

            fileEntries.Add(new BulkUploadFileEntry(
                ms,
                file.FileName,
                file.ContentType,
                null,
                null,
                null
            ));
        }

        var result = await _commands.Send<BulkUploadMusicCommand, BulkUploadMusicResult>(
            new BulkUploadMusicCommand(request.StationId, userId, fileEntries), ct);

        // Dispose all streams
        foreach (var entry in fileEntries)
        {
            await entry.FileStream.DisposeAsync();
        }

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

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// DEBUG: Get database statistics for troubleshooting
    /// </summary>
    [HttpGet("debug/stats")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> GetDebugStats(CancellationToken ct)
    {
        try
        {
            var allMusic = await _queries.Send<GetAllMediaFilesQuery, List<MusicResult>>(
                new GetAllMediaFilesQuery(), ct);

            var mediaFilesList = allMusic.Data?.Select(m => new
            {
                m.Id,
                m.Title,
                m.Artist,
                m.FileUrl
            }).ToList();

            var stats = new
            {
                TotalMediaFiles = allMusic.Data?.Count ?? 0,
                MediaFiles = (object?)(mediaFilesList) ?? new List<object>()
            };

            return Ok(ApiResponse<object>.SuccessResponse(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting debug stats");
            return StatusCode(500, ApiResponse<object>.FailureResponse(
                $"Error: {ex.Message}",
                500));
        }
    }
}
