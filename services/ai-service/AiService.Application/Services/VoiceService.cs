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
}

