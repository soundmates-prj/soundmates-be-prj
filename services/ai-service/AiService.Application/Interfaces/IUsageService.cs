using AiService.Domain.Entities;

namespace AiService.Application.Interfaces;

public interface IUsageService
{
    Task LogAsync(AiUsage usage, CancellationToken cancellationToken);
}

