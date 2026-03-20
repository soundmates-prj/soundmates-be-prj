using AiService.Domain.Entities;
using AiService.Domain.Interfaces;
using AiService.Infrastructure.Persistence;

namespace AiService.Infrastructure.Repositories;

public class AiPromptRepository : IAiPromptRepository
{
    private readonly AiDbContext _db;

    public AiPromptRepository(AiDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(AiPrompt prompt, CancellationToken cancellationToken)
        => _db.AiPrompts.AddAsync(prompt, cancellationToken).AsTask();
}

