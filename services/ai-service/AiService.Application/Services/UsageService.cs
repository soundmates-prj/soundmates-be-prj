using AiService.Application.Interfaces;
using AiService.Domain.Entities;
using AiService.Domain.Interfaces;

namespace AiService.Application.Services;

public class UsageService : IUsageService
{
    private readonly IAiUsageRepository _usage;
    private readonly IUnitOfWork _uow;

    public UsageService(IAiUsageRepository usage, IUnitOfWork uow)
    {
        _usage = usage;
        _uow = uow;
    }

    public async Task LogAsync(AiUsage usage, CancellationToken cancellationToken)
    {
        await _usage.AddAsync(usage, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}

