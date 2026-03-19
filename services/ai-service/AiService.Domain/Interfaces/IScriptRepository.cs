using AiService.Domain.Entities;

namespace AiService.Domain.Interfaces;

public interface IScriptRepository
{
    Task AddAsync(Script script, CancellationToken cancellationToken);
    Task<Script?> GetByIdAsync(Guid scriptId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Script>> GetByAuthorAsync(Guid authorId, string? contextType, string? status, CancellationToken cancellationToken);
    Task UpdateAsync(Script script, CancellationToken cancellationToken);
}

