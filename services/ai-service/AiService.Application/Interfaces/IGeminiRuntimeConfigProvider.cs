namespace AiService.Application.Interfaces;

public interface IGeminiRuntimeConfigProvider
{
    Task<string?> GetActiveApiKeyAsync(CancellationToken cancellationToken);

    Task UpdateAsync(
        string provider,
        string apiKey,
        bool isActive,
        DateTime updatedAt,
        CancellationToken cancellationToken);
}