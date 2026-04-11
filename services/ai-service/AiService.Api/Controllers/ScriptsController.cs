using AiService.Api.Extensions;
using AiService.Api.Models.Requests.Scripts;
using AiService.Api.Models.Responses;
using AiService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AiService.Application.Enums;
using AiService.Application.Features.Scripts.Commands.GeneratePodcastScript;
using AiService.Application.Features.Scripts.Commands.SplitScript;
using AiService.Application.Features.Scripts.Commands.DeleteScript;
using AiService.Application.Features.Scripts.Commands.UpdateScript;
using AiService.Application.Features.Scripts.Queries.GetMyScripts;
using AiService.Application.Features.Scripts.Queries.GetScriptById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScriptsController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public ScriptsController(ICommandDispatcher commands, IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    /// <summary>
    /// Generate a podcast script from topic and prompt options.
    /// </summary>
    /// <remarks>
    /// Use <c>EditorInstruction</c> to customize writing style.
    /// Set <c>UseAutoContext</c> and <c>StrictFactMode</c> to control generation behavior.
    /// </remarks>
    [HttpPost("podcast:generate")]
    public async Task<IActionResult> GeneratePodcast([FromBody] GeneratePodcastRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var result = await _commands.Send<GeneratePodcastScriptCommand, Domain.Entities.Script>(
            new GeneratePodcastScriptCommand(
                userId,
                request.Topic,
                request.Title,
                request.ContextType,
                request.ModelName,
                request.Temperature,
                request.MaxTokens,
                request.EditorInstruction,
                request.UseAutoContext,
                request.StrictFactMode),
            cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.SuccessResponse(new { script = result.Data }))
            : BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, result.ErrorMessage ?? "Failed"));
    }

    /// <summary>
    /// Split an existing script into smaller parts for audio processing.
    /// </summary>
    [HttpPost("{scriptId:guid}/split")]
    public async Task<IActionResult> Split([FromRoute] Guid scriptId, [FromBody] SplitScriptRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var result = await _commands.Send<SplitScriptPartsCommand, IReadOnlyList<Domain.Entities.Script>>(
            new SplitScriptPartsCommand(userId, scriptId, request.MaxCharsPerPart),
            cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.SuccessResponse(new { parts = result.Data }))
            : BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, result.ErrorMessage ?? "Failed"));
    }

    /// <summary>
    /// Get a script by its ID.
    /// </summary>
    [HttpGet("{scriptId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid scriptId, CancellationToken cancellationToken)
    {
        var result = await _queries.Send<GetScriptByIdQuery, Domain.Entities.Script>(new GetScriptByIdQuery(scriptId), cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.SuccessResponse(new { script = result.Data }))
            : NotFound(ApiResponse<string>.Error(ApiStatusCode.HB40401, result.ErrorMessage ?? "Not found"));
    }

    /// <summary>
    /// Get scripts created by the current user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] string? contextType, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var result = await _queries.Send<GetMyScriptsQuery, IReadOnlyList<Domain.Entities.Script>>(
            new GetMyScriptsQuery(userId, contextType, status),
            cancellationToken);

        return Ok(ApiResponse<object>.SuccessResponse(new { scripts = result.Data }));
    }

    /// <summary>
    /// Delete a script by ID. Only the script owner can delete it.
    /// </summary>
    [HttpDelete("{scriptId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid scriptId, CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var result = await _commands.Send<DeleteScriptCommand, bool>(
            new DeleteScriptCommand(userId, scriptId),
            cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<string>.SuccessResponse("Deleted"))
            : result.ErrorCode == (int)ApiStatusCode.HB40301
                ? Forbid()
                : NotFound(ApiResponse<string>.Error(ApiStatusCode.HB40401, result.ErrorMessage ?? "Not found"));
    }

    /// <summary>
    /// Update a script's title and/or content. Only the script owner can update it.
    /// </summary>
    [HttpPut("{scriptId:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid scriptId, [FromBody] UpdateScriptRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var result = await _commands.Send<UpdateScriptCommand, bool>(
            new UpdateScriptCommand(userId, scriptId, request.Title, request.ContentText),
            cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<string>.SuccessResponse("Updated"))
            : result.ErrorCode == (int)ApiStatusCode.HB40301
                ? Forbid()
                : NotFound(ApiResponse<string>.Error(ApiStatusCode.HB40401, result.ErrorMessage ?? "Not found"));
    }
}

