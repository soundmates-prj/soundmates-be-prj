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
    private readonly IPodcastPipelineService _pipeline;

    public PodcastController(IPodcastPipelineService pipeline)
    {
        _pipeline = pipeline;
    }

    [HttpPost("generate-full")]
    public async Task<IActionResult> GenerateFull([FromBody] GenerateFullPodcastRequest request, CancellationToken ct)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var result = await _pipeline.GenerateAndSyncPodcastAsync(
            new Application.Interfaces.GeneratePodcastRequest(
                UserId: userId,
                Topic: request.Topic,
                Title: request.Title,
                ContextType: "podcast",
                VoiceId: request.VoiceId,
                Speed: request.Speed,
                Pitch: request.Pitch,
                ModelName: request.ModelName
            ), ct);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, result.ErrorMessage ?? "Pipeline failed"));
        }

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            script = result.Data!.Script,
            audio = result.Data!.Audio,
            synced = result.Data!.SyncedToLiveService,
            externalId = result.Data!.LiveServicePodcastId
        }));
    }
}

public class GenerateFullPodcastRequest
{
    public string Topic { get; set; } = null!;
    public string? Title { get; set; }
    public Guid VoiceId { get; set; }
    public decimal? Speed { get; set; } = 1.0m;
    public decimal? Pitch { get; set; } = 1.0m;
    public string? ModelName { get; set; }
}
