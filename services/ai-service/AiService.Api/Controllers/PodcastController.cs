using AiService.Api.Extensions;
using AiService.Api.Models.Responses;
using AiService.Application.Enums;
using AiService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiService.Api.Controllers;

[ApiController]
[Route("api/v1/podcasts")]
[Authorize]
public class PodcastController : ControllerBase
{
    private readonly IPodcastGenerationService _podcastGenerationService;

    public PodcastController(IPodcastGenerationService podcastGenerationService)
    {
        _podcastGenerationService = podcastGenerationService;
    }

    [HttpPost("generate-full")]
    public async Task<IActionResult> GenerateFull([FromBody] GenerateFullPodcastRequest request, CancellationToken ct)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            return BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, "topic is required"));
        }

        if (string.IsNullOrWhiteSpace(request.Voice))
        {
            return BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, "voice is required"));
        }

        var result = await _podcastGenerationService.GeneratePodcastAudioAsync(
            new PodcastGenerateRequest(
                UserId: userId,
                Topic: request.Topic,
                Style: request.Style,
                Duration: request.Duration,
                Voice: request.Voice,
                Language: request.Language,
                ModelName: request.ModelName,
                IncludeAudioBytes: request.IncludeAudioBytes
            ), ct);

        if (!result.IsSuccess)
        {
            var apiCode = result.ErrorCode switch
            {
                (int)ApiStatusCode.HB40401 => ApiStatusCode.HB40401,
                (int)ApiStatusCode.HB50001 => ApiStatusCode.HB50001,
                _ => ApiStatusCode.HB40001
            };

            var payload = ApiResponse<string>.Error(apiCode, result.ErrorMessage ?? "Podcast generation failed");

            return apiCode switch
            {
                ApiStatusCode.HB40401 => NotFound(payload),
                ApiStatusCode.HB50001 => StatusCode(StatusCodes.Status500InternalServerError, payload),
                _ => BadRequest(payload)
            };
        }

        return Ok(ApiResponse<PodcastGenerateResult>.SuccessResponse(result.Data!));
    }
}

public class GenerateFullPodcastRequest
{
    public string Topic { get; set; } = null!;
    public string? Style { get; set; }
    public string? Duration { get; set; }
    public string Voice { get; set; } = null!;
    public string? Language { get; set; }
    public string? ModelName { get; set; }
    public bool IncludeAudioBytes { get; set; } = true;
}
