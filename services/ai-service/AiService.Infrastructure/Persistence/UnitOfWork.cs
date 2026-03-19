using AiService.Domain.Interfaces;

namespace AiService.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AiDbContext _db;

    public UnitOfWork(AiDbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _db.SaveChangesAsync(cancellationToken);
}

