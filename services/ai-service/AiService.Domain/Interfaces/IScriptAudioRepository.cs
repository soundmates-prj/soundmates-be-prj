using AiService.Domain.Entities;

namespace AiService.Domain.Interfaces;

public interface IScriptAudioRepository
{
    Task AddAsync(ScriptAudio audio, CancellationToken cancellationToken);
    Task<ScriptAudio?> GetByIdAsync(Guid audioId, CancellationToken cancellationToken);
    Task UpdateAsync(ScriptAudio audio, CancellationToken cancellationToken);
}

