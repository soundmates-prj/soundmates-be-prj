using AiService.Domain.Entities;
using AiService.Domain.Interfaces;
using AiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiService.Infrastructure.Repositories;

public class ScriptRepository : IScriptRepository
{
    private readonly AiDbContext _db;

    public ScriptRepository(AiDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(Script script, CancellationToken cancellationToken)
        => _db.Scripts.AddAsync(script, cancellationToken).AsTask();

    public Task<Script?> GetByIdAsync(Guid scriptId, CancellationToken cancellationToken)
        => _db.Scripts
            .Include(s => s.Audios)
            .FirstOrDefaultAsync(s => s.ScriptId == scriptId, cancellationToken);

    public async Task<IReadOnlyList<Script>> GetByAuthorAsync(Guid authorId, string? contextType, string? status, CancellationToken cancellationToken)
    {
        var q = _db.Scripts.AsNoTracking().Where(s => s.AuthorId == authorId);

        if (!string.IsNullOrWhiteSpace(contextType))
            q = q.Where(s => s.ContextType == contextType);
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(s => s.Status == status);

        return await q
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(Script script, CancellationToken cancellationToken)
    {
        _db.Scripts.Update(script);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid scriptId, CancellationToken cancellationToken)
    {
        var script = await _db.Scripts.FindAsync(new object[] { scriptId }, cancellationToken);
        if (script is not null)
            _db.Scripts.Remove(script);
    }
}

