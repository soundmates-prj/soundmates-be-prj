using AiService.Api.Extensions;
using AiService.Api.Models.Requests.Voices;
using AiService.Api.Models.Responses;
using AiService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AiService.Application.Enums;
using AiService.Application.Features.Voices.Commands.CreateVoice;
using AiService.Application.Features.Voices.Queries.GetActiveVoices;
using AiService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VoicesController : ControllerBase
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public VoicesController(ICommandDispatcher commands, IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var result = await _queries.Send<GetActiveVoicesQuery, IReadOnlyList<TtsVoice>>(new GetActiveVoicesQuery(), cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(new { voices = result.Data }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVoiceRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.VoiceType) || request.VoiceType == 0)
            return BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, "voiceType is invalid"));

        if (request.VoiceType == VoiceType.BuiltIn && !User.IsInRole("ADMIN"))
            return Forbid();

        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var isUserVoice = request.VoiceType == VoiceType.User;
        var result = await _commands.Send<CreateVoiceCommand, TtsVoice>(
            new CreateVoiceCommand(
                UserId: userId,
                IsUserVoice: isUserVoice,
                Provider: request.Provider,
                VoiceCode: request.VoiceCode,
                DisplayName: request.DisplayName,
                Region: request.Region,
                Gender: request.Gender,
                Model: request.Model,
                IsActive: request.IsActive),
            cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.SuccessResponse(new { voice = result.Data }))
            : BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, result.ErrorMessage ?? "Failed"));
    }
}

