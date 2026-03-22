namespace AccountContentService.Application.Interfaces.Services;

public interface IGeminiConfigEventPublisher
{
    Task PublishUpdatedAsync(string provider, string apiKey, bool isActive, DateTime updatedAt, CancellationToken cancellationToken);
}
