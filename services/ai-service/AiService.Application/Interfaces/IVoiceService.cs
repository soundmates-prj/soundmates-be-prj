using AiService.Application.Results;
using AiService.Domain.Entities;

namespace AiService.Application.Interfaces;

public interface IVoiceService
{
    Task<Result<IReadOnlyList<TtsVoice>>> GetActiveAsync(CancellationToken cancellationToken);
    Task<Result<TtsVoice>> CreateAsync(TtsVoice voice, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteAsync(Guid userId, Guid voiceId, CancellationToken cancellationToken);
}

