using AiService.Domain.Entities;

namespace AiService.Domain.Interfaces;

public interface IAiServiceConfigRepository
{
    Task<AiServiceConfig?> GetActiveByProviderAsync(string provider, CancellationToken cancellationToken);

    Task UpsertAsync(
        string provider,
        string apiKey,
        bool isActive,
        DateTime updatedAt,
        CancellationToken cancellationToken);
}
