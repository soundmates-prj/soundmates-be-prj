using AiService.Application.Results;
using AiService.Domain.Entities;

namespace AiService.Application.Interfaces;

public record GenerateAudioFromScriptRequest(
    Guid UserId,
    Guid ScriptId,
    Guid VoiceId,
    decimal? Speed,
    decimal? Pitch);

public interface IAudioService
{
    Task<Result<ScriptAudio>> GenerateAsync(GenerateAudioFromScriptRequest request, CancellationToken cancellationToken);
    Task<Result<ScriptAudio>> GetByIdAsync(Guid audioId, CancellationToken cancellationToken);
}

