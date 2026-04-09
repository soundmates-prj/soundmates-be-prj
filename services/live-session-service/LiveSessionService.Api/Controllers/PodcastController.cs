using LiveSessionService.Api.Extensions;
using LiveSessionService.Api.Models.Requests.Podcasts;
using LiveSessionService.Api.Models.Responses;
using LiveSessionService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using LiveSessionService.Application.Enums;
using LiveSessionService.Application.Features.Podcasts.Commands.CreatePodcast;
using LiveSessionService.Application.Features.Podcasts.Commands.CreatePodcastEpisode;
using LiveSessionService.Application.Features.Podcasts.Commands.DeletePodcast;
using LiveSessionService.Application.Features.Podcasts.Commands.DeletePodcastEpisode;
using LiveSessionService.Application.Features.Podcasts.Commands.UpdatePodcast;
using LiveSessionService.Application.Features.Podcasts.Commands.UpdatePodcastEpisode;
using LiveSessionService.Application.Features.Podcasts.Queries.GetPodcast;
using LiveSessionService.Application.Features.Podcasts.Queries.GetPodcastEpisodeById;
using LiveSessionService.Application.Features.Podcasts.Queries.GetPodcastEpisodes;
using LiveSessionService.Application.Features.Podcasts.Queries.GetPodcasts;
using LiveSessionService.Application.Features.Results.Podcasts;
using LiveSessionService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TagLib;

namespace LiveSessionService.Api.Controllers;

/// <summary>
/// API endpoints for Podcast management
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class PodcastController : ControllerBase
{
    private static readonly string[] AllowedAudioExtensions = [".mp3", ".flac", ".wav", ".ogg"];
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public PodcastController(ICommandDispatcher commands, IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    /// <summary>
    /// Retrieves a list of podcasts with optional filtering by creator and status
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PodcastResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? createdBy,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var query = new GetPodcastsQuery(createdBy, status);
        var result = await _queries.Send<GetPodcastsQuery, List<PodcastResult>>(query, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(ApiResponse<List<PodcastResult>>.SuccessResponse(result.Data!, $"Retrieved {result.Data!.Count} podcast(s)"));
    }

    /// <summary>
    /// Retrieves a specific podcast by its ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PodcastResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetPodcastQuery(id);
        var result = await _queries.Send<GetPodcastQuery, PodcastResult>(query, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Creates a new podcast
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PodcastResult>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> Create([FromBody] CreatePodcastRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.FailureResponse(
                "Invalid or missing user token",
                (int)ErrorCode.Unauthorized));
        }

        var command = new CreatePodcastCommand(
            userId,
            request.Title,
            request.Description,
            request.Author,
            request.Type,
            request.Banner);

        var result = await _commands.Send<CreatePodcastCommand, PodcastResult>(command, ct);

        if (!result.IsSuccess)
        {
            return StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse());
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.ToApiResponse());
    }

    /// <summary>
    /// Updates an existing podcast by its ID
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PodcastResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePodcastRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        var command = new UpdatePodcastCommand(
            id,
            request.Title,
            request.Description,
            request.Author,
            request.Type,
            request.Banner,
            request.Status);

        var result = await _commands.Send<UpdatePodcastCommand, PodcastResult>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Updates only the status of an existing podcast by its ID
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<PodcastResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePodcastStatusRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        if (!TryResolvePodcastStatus(request.Status, out var resolvedStatus))
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid podcast status",
                (int)ErrorCode.BadRequest));
        }

        var existing = await _queries.Send<GetPodcastQuery, PodcastResult>(new GetPodcastQuery(id), ct);
        if (!existing.IsSuccess)
        {
            return existing.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(existing.ToApiResponse()),
                _ => StatusCode((int)(existing.ErrorCode ?? ErrorCode.InternalServerError), existing.ToApiResponse())
            };
        }

        var podcast = existing.Data!;
        var command = new UpdatePodcastCommand(
            id,
            podcast.Title,
            podcast.Description,
            podcast.Author,
            podcast.Type,
            podcast.Banner,
            resolvedStatus.ToString());

        var result = await _commands.Send<UpdatePodcastCommand, PodcastResult>(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Deletes a podcast by its ID
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var command = new DeletePodcastCommand(id);
        var result = await _commands.Send(command, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode == ErrorCode.NotFound
                ? NotFound(result.ToApiResponse())
                : StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse());
        }

        return NoContent();
    }

    /// <summary>
    /// Retrieves a list of episodes for a specific podcast
    /// </summary>
    [HttpGet("{podcastId:guid}/episodes")]
    [ProducesResponseType(typeof(ApiResponse<List<PodcastEpisodeResult>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetEpisodes(Guid podcastId, CancellationToken ct)
    {
        var result = await _queries.Send<GetPodcastEpisodesQuery, List<PodcastEpisodeResult>>(
            new GetPodcastEpisodesQuery(podcastId),
            ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Retrieves a specific episode by its ID for a given podcast
    /// </summary>
    [HttpGet("{podcastId:guid}/episodes/{episodeId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PodcastEpisodeResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetEpisodeById(Guid podcastId, Guid episodeId, CancellationToken ct)
    {
        var result = await _queries.Send<GetPodcastEpisodeByIdQuery, PodcastEpisodeResult>(
            new GetPodcastEpisodeByIdQuery(podcastId, episodeId),
            ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return Ok(result.ToApiResponse());
    }

    /// <summary>
    /// Creates a new episode for a specific podcast
    /// </summary>
    [HttpPost("{podcastId:guid}/episodes")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<PodcastEpisodeResult>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> CreateEpisode(Guid podcastId, [FromForm] CreatePodcastEpisodeRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        var resolvedAudioUrl = request.AudioUrl?.Trim();
        int? resolvedDuration = request.Duration;

        try
        {
            if (request.AudioFile is not null)
            {
                var uploadedAudio = await SaveAudioFileAsync(request.AudioFile, ct);
                resolvedAudioUrl = uploadedAudio.AudioUrl;
                resolvedDuration = uploadedAudio.Duration;
            }

            if (string.IsNullOrWhiteSpace(resolvedAudioUrl))
            {
                return BadRequest(ApiResponse<object>.FailureResponse(
                    "AudioUrl or AudioFile is required",
                    (int)ErrorCode.BadRequest));
            }

            var resolvedThumbnailUrl = request.ThumbnailUrl?.Trim();
            if (request.ThumbnailFile is not null)
            {
                resolvedThumbnailUrl = await SaveImageFileAsync(request.ThumbnailFile, "podcast-thumbnails", AllowedImageExtensions, ct);
            }

            var result = await _commands.Send<CreatePodcastEpisodeCommand, PodcastEpisodeResult>(
                new CreatePodcastEpisodeCommand(
                    podcastId,
                    request.Title,
                    request.Description,
                    resolvedAudioUrl,
                    resolvedThumbnailUrl,
                    request.EpisodeNumber,
                    request.PublishDate,
                    resolvedDuration),
                ct);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                    ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                    ErrorCode.Conflict => Conflict(result.ToApiResponse()),
                    _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
                };
            }

            return CreatedAtAction(nameof(GetEpisodeById), new { podcastId, episodeId = result.Data!.Id }, result.ToApiResponse());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(ex.Message, (int)ErrorCode.BadRequest));
        }
    }

    /// <summary>
    /// Updates an existing episode for a specific podcast
    /// </summary>
    [HttpPut("{podcastId:guid}/episodes/{episodeId:guid}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<PodcastEpisodeResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> UpdateEpisode(Guid podcastId, Guid episodeId, [FromForm] UpdatePodcastEpisodeRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(
                "Invalid input",
                (int)ErrorCode.BadRequest));
        }

        string? resolvedAudioUrl = null;
        int? resolvedDuration = request.Duration;

        try
        {
            if (request.AudioFile is not null)
            {
                var uploadedAudio = await SaveAudioFileAsync(request.AudioFile, ct);
                resolvedAudioUrl = uploadedAudio.AudioUrl;
                resolvedDuration = uploadedAudio.Duration;
            }
            else if (!string.IsNullOrWhiteSpace(request.AudioUrl))
            {
                resolvedAudioUrl = request.AudioUrl.Trim();
            }

            string? resolvedThumbnailUrl = null;
            if (request.ThumbnailFile is not null)
            {
                resolvedThumbnailUrl = await SaveImageFileAsync(request.ThumbnailFile, "podcast-thumbnails", AllowedImageExtensions, ct);
            }
            else if (request.ThumbnailUrl is not null)
            {
                resolvedThumbnailUrl = request.ThumbnailUrl.Trim();
            }

            var result = await _commands.Send<UpdatePodcastEpisodeCommand, PodcastEpisodeResult>(
                new UpdatePodcastEpisodeCommand(
                    podcastId,
                    episodeId,
                    request.Title,
                    request.Description,
                    resolvedAudioUrl,
                    resolvedThumbnailUrl,
                    request.EpisodeNumber,
                    request.PublishDate,
                    resolvedDuration),
                ct);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                    ErrorCode.BadRequest => BadRequest(result.ToApiResponse()),
                    ErrorCode.Conflict => Conflict(result.ToApiResponse()),
                    _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
                };
            }

            return Ok(result.ToApiResponse());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.FailureResponse(ex.Message, (int)ErrorCode.BadRequest));
        }
    }

    /// <summary>
    /// Deletes an episode by its ID for a given podcast
    /// </summary>
    [HttpDelete("{podcastId:guid}/episodes/{episodeId:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteEpisode(Guid podcastId, Guid episodeId, CancellationToken ct)
    {
        var result = await _commands.Send(new DeletePodcastEpisodeCommand(podcastId, episodeId), ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.NotFound => NotFound(result.ToApiResponse()),
                _ => StatusCode((int)(result.ErrorCode ?? ErrorCode.InternalServerError), result.ToApiResponse())
            };
        }

        return NoContent();
    }

    private static async Task<(string AudioUrl, int Duration)> SaveAudioFileAsync(IFormFile file, CancellationToken ct)
    {
        var extension = ValidateAndGetExtension(file.FileName, AllowedAudioExtensions, "audio file");
        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = $"podcast-media/{safeFileName}";
        var absoluteDir = Path.Combine(AppContext.BaseDirectory, "storage", "podcast-media");
        Directory.CreateDirectory(absoluteDir);
        var absolutePath = Path.Combine(absoluteDir, safeFileName);

        await using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        var duration = ExtractAudioDuration(memoryStream, file.FileName);
        memoryStream.Position = 0;

        await using (var fs = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await memoryStream.CopyToAsync(fs, ct);
        }

        return ($"system://{relativePath}", duration);
    }

    private static async Task<string> SaveImageFileAsync(IFormFile file, string directoryName, string[] allowedExtensions, CancellationToken ct)
    {
        var extension = ValidateAndGetExtension(file.FileName, allowedExtensions, "image file");
        var safeFileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = $"{directoryName}/{safeFileName}";
        var absoluteDir = Path.Combine(AppContext.BaseDirectory, "storage", directoryName);
        Directory.CreateDirectory(absoluteDir);
        var absolutePath = Path.Combine(absoluteDir, safeFileName);

        await using var stream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, ct);

        return $"system://{relativePath}";
    }

    private static string ValidateAndGetExtension(string fileName, string[] allowedExtensions, string fileType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"Unsupported {fileType} type. Allowed: {string.Join(", ", allowedExtensions)}");
        }

        return extension;
    }

    private static int ExtractAudioDuration(Stream stream, string fileName)
    {
        try
        {
            stream.Position = 0;
            var abstraction = new TagLibStreamAbstraction(stream, fileName);
            using var tagFile = TagLib.File.Create(abstraction);
            var seconds = (int)Math.Round(tagFile.Properties.Duration.TotalSeconds);
            return Math.Max(0, seconds);
        }
        catch
        {
            return 0;
        }
    }

    private static bool TryResolvePodcastStatus(string rawStatus, out PodcastStatus status)
    {
        var normalized = rawStatus.Trim();

        if (normalized.Equals("public", StringComparison.OrdinalIgnoreCase))
        {
            status = PodcastStatus.Published;
            return true;
        }

        if (normalized.Equals("private", StringComparison.OrdinalIgnoreCase))
        {
            status = PodcastStatus.Draft;
            return true;
        }

        return Enum.TryParse(normalized, true, out status);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? User.FindFirst("sub")
            ?? User.FindFirst("user_id");

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedUserId))
        {
            userId = parsedUserId;
            return true;
        }

        userId = Guid.Empty;
        return false;
    }
}

internal sealed class TagLibStreamAbstraction : TagLib.File.IFileAbstraction
{
    private readonly Stream _stream;
    private readonly string _fileName;

    public TagLibStreamAbstraction(Stream stream, string fileName)
    {
        _stream = stream;
        _fileName = fileName;
    }

    public string Name => _fileName;
    public Stream ReadStream => _stream;

    public Stream WriteStream
    {
        get
        {
            _stream.Position = 0;
            return _stream;
        }
    }

    public void CloseStream(Stream stream)
    {
    }
}
