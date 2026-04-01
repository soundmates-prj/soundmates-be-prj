using AiService.Application.Results;
using AiService.Domain.Entities;

namespace AiService.Application.Interfaces;

public interface IVoiceService
{
    Task<Result<IReadOnlyList<TtsVoice>>> GetActiveAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Lay danh sach gioc cua mot nguoi dung (bao gom built-in + user-created)
    /// </summary>
    Task<Result<IReadOnlyList<TtsVoice>>> GetByUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<TtsVoice>> CreateAsync(TtsVoice voice, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteAsync(Guid userId, Guid voiceId, CancellationToken cancellationToken);
}

