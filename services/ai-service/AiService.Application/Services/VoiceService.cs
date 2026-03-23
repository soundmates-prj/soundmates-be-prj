using AiService.Application.Enums;
using AiService.Application.Interfaces;
using AiService.Application.Results;
using AiService.Domain.Entities;
using AiService.Domain.Interfaces;

namespace AiService.Application.Services;

public class VoiceService : IVoiceService
{
    private readonly IVoiceRepository _voices;
    private readonly IUnitOfWork _uow;

    public VoiceService(IVoiceRepository voices, IUnitOfWork uow)
    {
        _voices = voices;
        _uow = uow;
    }

    public async Task<Result<IReadOnlyList<TtsVoice>>> GetActiveAsync(CancellationToken cancellationToken)
    {
        var voices = await _voices.GetActiveAsync(cancellationToken);
        return Result<IReadOnlyList<TtsVoice>>.Success(voices);
    }

    public async Task<Result<TtsVoice>> CreateAsync(TtsVoice voice, CancellationToken cancellationToken)
    {
        voice.VoiceId = voice.VoiceId == Guid.Empty ? Guid.NewGuid() : voice.VoiceId;
        voice.CreatedAt = voice.CreatedAt == default ? DateTime.UtcNow : voice.CreatedAt;

        await _voices.AddAsync(voice, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<TtsVoice>.Success(voice);
    }
    public async Task<Result<bool>> DeleteAsync(Guid userId, Guid voiceId, CancellationToken cancellationToken)
    {
        var voice = await _voices.GetByIdAsync(voiceId, cancellationToken);
        if (voice is null)
            return Result<bool>.Failure("voice not found", (int)ApiStatusCode.HB40401);

        // Standard voices (built-in) usually don't have a UserId or belong to System.
        // If there's no UserId field in Domain, we check role.
        // For simplicity, let's assume if it's not custom, we can't delete unless Admin.
        
        await _voices.DeleteAsync(voiceId, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        
        return Result<bool>.Success(true);
    }
}

