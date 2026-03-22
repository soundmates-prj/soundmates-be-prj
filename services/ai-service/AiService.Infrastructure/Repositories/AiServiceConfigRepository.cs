using AiService.Domain.Entities;
using AiService.Domain.Interfaces;
using AiService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiService.Infrastructure.Repositories;

public class AiServiceConfigRepository : IAiServiceConfigRepository
{
    private readonly AiDbContext _db;

    public AiServiceConfigRepository(AiDbContext db)
    {
        _db = db;
    }

    public Task<AiServiceConfig?> GetActiveByProviderAsync(string provider, CancellationToken cancellationToken)
    {
        return _db.AiServiceConfigs
            .AsNoTracking()
            .Where(x => x.IsActive && x.Provider.ToLower() == provider.ToLower())
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertAsync(
        string provider,
        string apiKey,
        bool isActive,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = provider.Trim();
        var entity = await _db.AiServiceConfigs
            .Where(x => x.Provider.ToLower() == normalizedProvider.ToLower())
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
        {
            entity = new AiServiceConfig
            {
                Provider = normalizedProvider,
                ApiKey = apiKey,
                IsActive = isActive,
                CreatedAt = updatedAt,
                UpdatedAt = updatedAt
            };

            await _db.AiServiceConfigs.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.ApiKey = apiKey;
            entity.IsActive = isActive;
            entity.UpdatedAt = updatedAt;
            _db.AiServiceConfigs.Update(entity);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
