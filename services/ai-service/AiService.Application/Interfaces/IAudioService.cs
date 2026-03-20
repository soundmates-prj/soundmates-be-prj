using AiService.Application.Results;
using AiService.Domain.Entities;
using System.IO;

namespace AiService.Application.Interfaces;

public record GenerateAudioFromScriptRequest(
    Guid UserId,
    Guid ScriptId,
    Guid VoiceId,
    decimal? Speed,
    decimal? Pitch);

public record AudioFileStreamResult(
    Stream Stream,
    string ContentType,
    long? ContentLength);

public interface IAudioService
{
    Task<Result<ScriptAudio>> GenerateAsync(GenerateAudioFromScriptRequest request, CancellationToken cancellationToken);
    Task<Result<ScriptAudio>> GetByIdAsync(Guid audioId, CancellationToken cancellationToken);

    /// <summary>
    /// Opens the underlying audio stream only if the caller owns the audio.
    /// </summary>
    Task<Result<AudioFileStreamResult>> OpenReadForUserAsync(Guid userId, Guid audioId, CancellationToken cancellationToken);
}

