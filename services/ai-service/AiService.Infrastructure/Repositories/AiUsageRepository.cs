using AiService.Domain.Entities;
using AiService.Domain.Interfaces;
using AiService.Infrastructure.Persistence;

namespace AiService.Infrastructure.Repositories;

public class AiUsageRepository : IAiUsageRepository
{
    private readonly AiDbContext _db;

    public AiUsageRepository(AiDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(AiUsage usage, CancellationToken cancellationToken)
        => _db.AiUsages.AddAsync(usage, cancellationToken).AsTask();
}

