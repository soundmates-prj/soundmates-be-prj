using AccountContentService.Domain.Entities;

namespace AccountContentService.Application.Interfaces.Repositories;

public interface IServiceConfigRepository
{
    Task<ServiceConfig?> GetByProviderAsync(string provider, CancellationToken cancellationToken);

    Task<ServiceConfig> UpsertAsync(
        string provider,
        string apiKey,
        bool isActive,
        DateTime updatedAt,
        CancellationToken cancellationToken);
}
