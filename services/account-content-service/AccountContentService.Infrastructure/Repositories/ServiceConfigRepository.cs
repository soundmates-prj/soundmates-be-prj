using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountContentService.Infrastructure.Repositories;

public sealed class ServiceConfigRepository : IServiceConfigRepository
{
    private readonly AccountContentDbContext _dbContext;

    public ServiceConfigRepository(AccountContentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ServiceConfig?> GetByProviderAsync(string provider, CancellationToken cancellationToken)
    {
        return _dbContext.ServiceConfigs
            .AsNoTracking()
            .Where(x => x.Provider.ToLower() == provider.ToLower())
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceConfig> UpsertAsync(
        string provider,
        string apiKey,
        bool isActive,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.ServiceConfigs
            .Where(x => x.Provider.ToLower() == provider.ToLower())
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            existing = new ServiceConfig
            {
                Provider = provider,
                ApiKey = apiKey,
                IsActive = isActive,
                CreatedAt = updatedAt,
                UpdatedAt = updatedAt
            };

            await _dbContext.ServiceConfigs.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.ApiKey = apiKey;
            existing.IsActive = isActive;
            existing.UpdatedAt = updatedAt;
            _dbContext.ServiceConfigs.Update(existing);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }
}
