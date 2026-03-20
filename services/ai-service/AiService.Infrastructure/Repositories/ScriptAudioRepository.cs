using AiService.Domain.Entities;
using AiService.Domain.Interfaces;
using AiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiService.Infrastructure.Repositories;

public class ScriptAudioRepository : IScriptAudioRepository
{
    private readonly AiDbContext _db;

    public ScriptAudioRepository(AiDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(ScriptAudio audio, CancellationToken cancellationToken)
        => _db.ScriptAudios.AddAsync(audio, cancellationToken).AsTask();

    public Task<ScriptAudio?> GetByIdAsync(Guid audioId, CancellationToken cancellationToken)
        => _db.ScriptAudios
            .Include(a => a.Script)
            .Include(a => a.Voice)
            .FirstOrDefaultAsync(a => a.AudioId == audioId, cancellationToken);

    public Task UpdateAsync(ScriptAudio audio, CancellationToken cancellationToken)
    {
        _db.ScriptAudios.Update(audio);
        return Task.CompletedTask;
    }
}

