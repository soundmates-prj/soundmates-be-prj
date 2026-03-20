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

namespace AiService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AudiosController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;
    private readonly IAudioService _audioService;

    public AudiosController(ICommandDispatcher commands, IQueryDispatcher queries, IAudioService audioService)
    {
        _commands = commands;
        _queries = queries;
        _audioService = audioService;
    }

    [HttpPost("/api/scripts/{scriptId:guid}/audio:generate")]
    public async Task<IActionResult> Generate([FromRoute] Guid scriptId, [FromBody] GenerateAudioRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var result = await _commands.Send<GenerateAudioFromScriptCommand, Domain.Entities.ScriptAudio>(
            new GenerateAudioFromScriptCommand(userId, scriptId, request.VoiceId, request.Speed, request.Pitch),
            cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.SuccessResponse(new { audio = result.Data }))
            : BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, result.ErrorMessage ?? "Failed"));
    }

    [HttpGet("{audioId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid audioId, CancellationToken cancellationToken)
    {
        var result = await _queries.Send<GetAudioByIdQuery, Domain.Entities.ScriptAudio>(new GetAudioByIdQuery(audioId), cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponse<object>.SuccessResponse(new { audio = result.Data }))
            : NotFound(ApiResponse<string>.Error(ApiStatusCode.HB40401, result.ErrorMessage ?? "Not found"));
    }

    [HttpGet("{audioId:guid}/file")]
    public async Task<IActionResult> GetFile([FromRoute] Guid audioId, CancellationToken cancellationToken)
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

        if (openResult.Data.ContentLength is long len)
            Response.ContentLength = len;

        return File(openResult.Data.Stream, openResult.Data.ContentType, enableRangeProcessing: true);
    }
}

