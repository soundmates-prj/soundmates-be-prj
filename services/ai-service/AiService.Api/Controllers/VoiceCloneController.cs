using System.Linq;
using AiService.Api.Extensions;
using AiService.Api.Models.Responses;
using AiService.Application.Enums;
using AiService.Application.Interfaces;
using AiService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiService.Api.Controllers;

/// <summary>
/// Controller xu ly voice cloning: upload audio nguon → clone thanh gioc AI
/// </summary>
[ApiController]
[Route("api/voice-clone")]
[Authorize]
public class VoiceCloneController : ControllerBase
{
    private readonly ITextToSpeechService _tts;
    private readonly IVoiceService _voiceService;
    private readonly IAudioStorage _storage;
    private readonly ILogger<VoiceCloneController> _logger;

    public VoiceCloneController(
        ITextToSpeechService tts,
        IVoiceService voiceService,
        IAudioStorage storage,
        ILogger<VoiceCloneController> logger)
    {
        _tts = tts;
        _voiceService = voiceService;
        _storage = storage;
        _logger = logger;
    }

    /// <summary>
    /// Clone a new voice from uploaded audio sample
    /// </summary>
    [HttpPost("clone")]
    public async Task<IActionResult> Clone(
        [FromForm] string displayName,
        [FromForm] string refText,
        [FromForm] string gender,
        IFormFile file,
        CancellationToken ct)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, "file is required"));

        if (string.IsNullOrWhiteSpace(displayName))
            return BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, "displayName is required"));

        if (string.IsNullOrWhiteSpace(refText))
            return BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, "refText is required - this is the transcription of the uploaded audio"));

        // Validate file type
        var allowedExtensions = new[] { ".wav", ".mp3", ".m4a", ".flac", ".ogg" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001,
                $"Unsupported file type. Allowed: {string.Join(", ", allowedExtensions)}"));

        // Max 10MB
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, "File size must be less than 10MB"));

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var audioBytes = ms.ToArray();

        // Generate unique voice ID
        var voiceId = $"user_{userId:N}_{Guid.NewGuid():N}";

        _logger.LogInformation("Starting voice clone for user {UserId}, voiceId: {VoiceId}", userId, voiceId);

        // Call VieNeu-TTS to clone voice
        var cloneResult = await _tts.CloneVoiceAsync(voiceId, refText, audioBytes, file.FileName, ct);

        if (!cloneResult.IsSuccess)
        {
            _logger.LogError("Voice clone failed: {Error}", cloneResult.ErrorMessage);
            return BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB50001,
                cloneResult.ErrorMessage ?? "Failed to clone voice"));
        }

        // Save the source audio to storage for reference
        string? sourceAudioUrl = null;
        try
        {
            var stored = await _storage.SaveAsync(
                fileNameWithoutExtension: $"voice_source_{voiceId}",
                extensionWithDot: ext,
                contentType: file.ContentType,
                bytes: audioBytes,
                cancellationToken: ct);
            sourceAudioUrl = $"/api/voice-clone/audio/{voiceId}{ext}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save source audio for voice {VoiceId}", voiceId);
            // Non-critical, continue anyway
        }

        // Register the cloned voice in our DB
        var voice = new TtsVoice
        {
            VoiceId = Guid.NewGuid(),
            UserId = userId,
            Provider = "ViNeuTTS",
            VoiceCode = voiceId,
            DisplayName = displayName,
            Region = "VN",
            Gender = gender ?? "Unknown",
            Model = "custom-cloned",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _voiceService.CreateAsync(voice, ct);

        if (!createResult.IsSuccess)
        {
            _logger.LogError("Failed to save cloned voice to DB: {Error}", createResult.ErrorMessage);
            return BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB50001, "Failed to save voice"));
        }

        _logger.LogInformation("Voice clone successful: {VoiceId} for user {UserId}", voiceId, userId);

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            voice = createResult.Data,
            voiceId = voiceId,
            sourceAudioUrl = sourceAudioUrl,
            message = "Voice cloned successfully. You can now use this voice for TTS."
        }));
    }

    /// <summary>
    /// Get the source audio for a cloned voice
    /// </summary>
    [HttpGet("audio/{voiceId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAudio(string voiceId, CancellationToken ct)
    {
        // Simple: just try to open from storage
        // In production you'd validate ownership
        var exts = new[] { ".wav", ".mp3", ".m4a", ".flac", ".ogg" };

        foreach (var ext in exts)
        {
            var path = $"voice_source_{voiceId}{ext}";
            try
            {
                var (stream, contentType, _) = await _storage.OpenReadAsync(path, ct);
                return File(stream, contentType);
            }
            catch { continue; }
        }

        return NotFound(ApiResponse<string>.Error(ApiStatusCode.HB40401, "Audio not found"));
    }

    /// <summary>
    /// List user's cloned voices
    /// </summary>
    [HttpGet("my-voices")]
    public async Task<IActionResult> GetMyVoices(CancellationToken ct)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var result = await _voiceService.GetByUserAsync(userId, ct);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.SuccessResponse(new { voices = result.Data }))
            : BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, result.ErrorMessage ?? "Failed"));
    }

    /// <summary>
    /// Delete a cloned voice
    /// </summary>
    [HttpDelete("{voiceId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid voiceId, CancellationToken ct)
    {
        if (!User.TryGetCurrentUserId(out var userId))
            return Unauthorized(ApiResponse<string>.Error(ApiStatusCode.HB40101, "Invalid token"));

        var result = await _voiceService.DeleteAsync(userId, voiceId, ct);

        return result.IsSuccess
            ? Ok(ApiResponse<string>.SuccessResponse("Voice deleted"))
            : BadRequest(ApiResponse<string>.Error(ApiStatusCode.HB40001, result.ErrorMessage ?? "Failed"));
    }
}
