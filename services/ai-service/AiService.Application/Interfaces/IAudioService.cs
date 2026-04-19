using AiService.Application.Results;
using AiService.Domain.Entities;
using System.IO;

namespace AiService.Application.Interfaces;

public record GenerateAudioFromScriptRequest(
    Guid UserId,
    Guid ScriptId,
    string VoiceCode,
    decimal? Speed,
    decimal? Pitch,
    string? BgmUrl);

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
    Task<Result<AudioFileStreamResult>> OpenReadAnonymousAsync(Guid audioId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ScriptAudio>>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteAsync(Guid userId, Guid audioId, CancellationToken cancellationToken);
}

