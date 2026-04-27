using AiService.Domain.Entities;

namespace AiService.Domain.Interfaces;

public interface IAiUsageRepository
{
    Task AddAsync(AiUsage usage, CancellationToken cancellationToken);
}

