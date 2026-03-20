using AiService.Domain.Entities;
using AiService.Domain.Interfaces;
using AiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiService.Infrastructure.Repositories;

public class VoiceRepository : IVoiceRepository
{
    private readonly AiDbContext _db;

    public VoiceRepository(AiDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(TtsVoice voice, CancellationToken cancellationToken)
        => _db.TtsVoices.AddAsync(voice, cancellationToken).AsTask();

    public Task<TtsVoice?> GetByIdAsync(Guid voiceId, CancellationToken cancellationToken)
        => _db.TtsVoices.FirstOrDefaultAsync(v => v.VoiceId == voiceId, cancellationToken);

    public async Task<IReadOnlyList<TtsVoice>> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await _db.TtsVoices
            .AsNoTracking()
            .Where(v => v.IsActive)
            .OrderBy(v => v.Provider)
            .ThenBy(v => v.DisplayName)
            .ToListAsync(cancellationToken);
    }
}

