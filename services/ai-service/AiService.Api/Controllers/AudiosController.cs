using System.Linq;
using AiService.Api.Extensions;
using AiService.Api.Models.Requests.Audios;
using AiService.Api.Models.Responses;
using AiService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AiService.Application.Enums;
using AiService.Application.Features.Audios.Commands.GenerateAudio;
using AiService.Application.Features.Audios.Queries.GetAudioById;
using AiService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AiService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AudiosController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;
    private readonly IAudioService _audioService;
    private readonly IConfiguration _configuration;

    public AudiosController(ICommandDispatcher commands, IQueryDispatcher queries, IAudioService audioService, IConfiguration configuration)
    {
        _commands = commands;
        _queries = queries;
        _audioService = audioService;
        _configuration = configuration;
    }

    [HttpPost("/api/scripts/{scriptId:guid}/audio:generate")]
    public async Task<IActionResult> Generate([FromRoute] Guid scriptId, [FromBody] GenerateAudioRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var result = await _commands.Send<GenerateAudioFromScriptCommand, Domain.Entities.ScriptAudio>(
            new GenerateAudioFromScriptCommand(userId, scriptId, request.VoiceCode, request.Speed, request.Pitch),
            cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.SuccessResponse(new { audio = MapToAudioResponse(result.Data) }))
            : BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, result.ErrorMessage ?? "Failed"));
    }

    [HttpGet("{audioId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid audioId, CancellationToken cancellationToken)
    {
        var result = await _queries.Send<GetAudioByIdQuery, Domain.Entities.ScriptAudio>(new GetAudioByIdQuery(audioId), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.SuccessResponse(new { audio = MapToAudioResponse(result.Data) }))
            : NotFound(ApiResponse<string>.Error(ApiStatusCode.HB40401, result.ErrorMessage ?? "Not found"));
    }

    [AllowAnonymous]
    [HttpGet("{audioId:guid}/file")]
    public async Task<IActionResult> GetFile([FromRoute] Guid audioId, CancellationToken cancellationToken)
    {
        var openResult = await _audioService.OpenReadAnonymousAsync(audioId, cancellationToken);
        if (!openResult.IsSuccess || openResult.Data is null)
        {
            return NotFound(ApiResponse<string>.Error(
                ApiStatusCode.HB40401,
                openResult.ErrorMessage ?? "audio not found"));
        }

        return File(openResult.Data.Stream, openResult.Data.ContentType, enableRangeProcessing: true);
    }

    /// <summary>
    /// Serves podcast audio files by filename (e.g. /api/audios/podcast-file/37f79ec16f384b2aa1d1cbe9367e9223.mp3).
    /// Used by the generate-full endpoint for direct browser playback.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("podcast-file/{fileName}")]
    public async Task<IActionResult> GetPodcastFile([FromRoute] string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains(".."))
            return BadRequest("Invalid filename");

        var root = ResolveAudioRoot();
        var fullPath = Path.Combine(root, fileName);

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound(ApiResponse<string>.Error(
                ApiStatusCode.HB40401,
                "Audio file not found"));
        }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = ext switch
        {
            ".wav" => "audio/wav",
            ".mp3" => "audio/mpeg",
            _ => "application/octet-stream"
        };

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
        return File(stream, contentType, enableRangeProcessing: true);
    }

    [HttpGet("{audioId:guid}/download")]
    public async Task<IActionResult> Download([FromRoute] Guid audioId, CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var openResult = await _audioService.OpenReadForUserAsync(userId, audioId, cancellationToken);
        if (!openResult.IsSuccess || openResult.Data is null)
        {
            if (openResult.ErrorCode == (int)ApiStatusCode.HB40301)
                return Forbid();

            return NotFound(ApiResponse<string>.Error(
                ApiStatusCode.HB40401,
                openResult.ErrorMessage ?? "audio not found"));
        }

        var result = await _queries.Send<GetAudioByIdQuery, Domain.Entities.ScriptAudio>(new GetAudioByIdQuery(audioId), cancellationToken);
        var fileName = result.IsSuccess && result.Data is not null
            ? Path.GetFileName(result.Data.AudioPath)
            : $"{audioId:N}.mp3";

        return File(openResult.Data.Stream, openResult.Data.ContentType, fileName, enableRangeProcessing: true);
    }

    private AudioResponse MapToAudioResponse(Domain.Entities.ScriptAudio audio)
    {
        var baseUrl = ResolvePublicBaseUrl();
        var streamPath = $"/api/audios/{audio.AudioId}/file";
        var downloadPath = $"/api/audios/{audio.AudioId}/download";
        var fileName = Path.GetFileName(audio.AudioPath);
        var contentType = ResolveContentType(fileName);
        var contentLength = ResolveContentLength(audio.AudioPath);

        return new AudioResponse
        {
            Id = audio.AudioId,
            FileName = fileName,
            ContentType = contentType,
            ContentLength = contentLength,
            StoragePath = audio.AudioPath,
            PublicUrl = CombineUrl(baseUrl, streamPath),
            DownloadUrl = CombineUrl(baseUrl, downloadPath),
            ScriptId = audio.ScriptId,
            VoiceId = audio.VoiceId,
            Speed = (float)(audio.Speed ?? 1.0m),
            Pitch = (float)(audio.Pitch ?? 1.0m),
            DurationSeconds = audio.Duration,
            CreatedAtUtc = audio.CreatedAt,
            UpdatedAtUtc = audio.UpdatedAt ?? audio.CreatedAt
        };
    }

    private string ResolvePublicBaseUrl()
    {
        var configured = _configuration["Storage:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured)
            && !configured.StartsWith("${", StringComparison.Ordinal)
            && Uri.TryCreate(configured, UriKind.Absolute, out _))
        {
            return configured.TrimEnd('/');
        }

        return $"{Request.Scheme}://{Request.Host.Value}".TrimEnd('/');
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        return $"{baseUrl}{path}";
    }

    private string ResolveContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".wav" => "audio/wav",
            ".mp3" => "audio/mpeg",
            _ => "application/octet-stream"
        };
    }

    private long ResolveContentLength(string relativePath)
    {
        var root = ResolveAudioRoot();
        if (string.IsNullOrWhiteSpace(root))
            return 0;

        var safe = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(root, safe);
        if (!System.IO.File.Exists(fullPath))
            return 0;

        return new FileInfo(fullPath).Length;
    }

    private string ResolveAudioRoot()
    {
        var configured = _configuration["Storage:AudioRoot"];
        if (!string.IsNullOrWhiteSpace(configured) && !configured.StartsWith("${", StringComparison.Ordinal))
            return configured;

        return Path.Combine(AppContext.BaseDirectory, "data", "audios");
    }
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var result = await _audioService.ListForUserAsync(userId, ct);
        var responses = result.Data?.Select(MapToAudioResponse) ?? Enumerable.Empty<AudioResponse>();
        return Ok(ApiResponse<object>.SuccessResponse(new { audios = responses }));
    }

    [HttpDelete("{audioId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid audioId, CancellationToken ct)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var result = await _audioService.DeleteAsync(userId, audioId, ct);
        return result.IsSuccess 
            ? Ok(ApiResponse<string>.SuccessResponse("Deleted"))
            : BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, result.ErrorMessage ?? "Failed"));
    }
}

