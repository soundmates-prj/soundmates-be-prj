using AiService.Domain.Entities;

namespace AiService.Domain.Interfaces;

public interface IVoiceRepository
{
    Task AddAsync(TtsVoice voice, CancellationToken cancellationToken);
    Task<TtsVoice?> GetByIdAsync(Guid voiceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TtsVoice>> GetActiveAsync(CancellationToken cancellationToken);
    Task DeleteAsync(Guid voiceId, CancellationToken cancellationToken);
    Task<TtsVoice?> GetByCodeAsync(string provider, string voiceCode, CancellationToken cancellationToken);
}

