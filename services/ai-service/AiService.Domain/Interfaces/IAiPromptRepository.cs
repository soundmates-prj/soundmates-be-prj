using AiService.Domain.Entities;

namespace AiService.Domain.Interfaces;

public interface IAiPromptRepository
{
    Task AddAsync(AiPrompt prompt, CancellationToken cancellationToken);
}

