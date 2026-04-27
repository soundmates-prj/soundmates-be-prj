using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using AiService.Application.Constants;
using AiService.Api.Extensions;
using AiService.Api.Models.Requests.Voices;
using AiService.Api.Models.Responses;
using AiService.Application.Abstractions.Messaging.Dispatcher.Interfaces;
using AiService.Application.Enums;
using AiService.Application.Features.Voices.Commands.CreateVoice;
using AiService.Application.Features.Voices.Queries.GetActiveVoices;
using AiService.Domain.Entities;
using AiService.Application.Interfaces;
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
    private readonly IVoiceService _voiceService;

    public VoicesController(ICommandDispatcher commands, IQueryDispatcher queries, IVoiceService voiceService)
    {
        _commands = commands;
        _queries = queries;
        _voiceService = voiceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var result = await _queries.Send<GetActiveVoicesQuery, IReadOnlyList<TtsVoice>>(new GetActiveVoicesQuery(), cancellationToken);

        // Lay them gioc cua nguoi dung hien tai
        var userVoicesResult = await _voiceService.GetByUserAsync(userId, cancellationToken);
        var userVoices = userVoicesResult.IsSuccess ? userVoicesResult.Data ?? [] : [];

        // Merge: loai bo trung lap (built-in co the da co trong GetActive)
        var allVoices = result.Data
            .Concat(userVoices)
            .DistinctBy(v => v.VoiceId)
            .ToList();

        return Ok(ApiResponse<object>.SuccessResponse(new { voices = allVoices }));
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

    [HttpPost("clone")]
    public async Task<IActionResult> Clone([FromForm] string voice_id, [FromForm] string ref_text, IFormFile file, CancellationToken ct)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, "file is required"));

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var audioBytes = ms.ToArray();

        // Register in TTS Provider
        var tts = HttpContext.RequestServices.GetRequiredService<ITextToSpeechService>();
        var result = await tts.CloneVoiceAsync(voice_id, ref_text, audioBytes, file.FileName, ct);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB50001, result.ErrorMessage ?? "Failed to clone voice"));
        }

        // Save association in our DB so it appears in GetActive list
        var voiceResult = await _commands.Send<CreateVoiceCommand, TtsVoice>(
            new CreateVoiceCommand(
                UserId: userId,
                IsUserVoice: true,
                Provider: AiProviderConstants.VieNeuTts,
                VoiceCode: voice_id,
                DisplayName: voice_id,
                Region: "VN",
                Gender: "Unknown",
                Model: "custom",
                IsActive: true),
            ct);

        return Ok(ApiResponse<object>.SuccessResponse(new { voice = voiceResult.Data }));
    }

    [HttpDelete("code/{voiceCode}")]
    public async Task<IActionResult> DeleteByCode([FromRoute] string voiceCode, CancellationToken ct)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        if (string.IsNullOrWhiteSpace(voiceCode))
            return BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, "voiceCode is required"));

        var voiceService = HttpContext.RequestServices.GetRequiredService<IVoiceService>();
        var result = await voiceService.DeleteByCodeAsync(userId, AiProviderConstants.VieNeuTts, voiceCode, ct);

        if (result.IsSuccess)
            return Ok(ApiResponse<string>.SuccessResponse("Deleted"));

        var statusCode = result.ErrorCode ?? (int)ApiStatusCode.HB40001;
        if (statusCode == (int)ApiStatusCode.HB40301)
            return StatusCode(403, ApiResponse<string>.Error((ApiStatusCode)statusCode, result.ErrorMessage ?? "Forbidden"));
            
        if (statusCode == (int)ApiStatusCode.HB40401)
            return NotFound(ApiResponse<string>.Error((ApiStatusCode)statusCode, result.ErrorMessage ?? "Not Found"));

        return BadRequest(ApiResponse<string>.Error((ApiStatusCode)statusCode, result.ErrorMessage ?? "Failed"));
    }
}

