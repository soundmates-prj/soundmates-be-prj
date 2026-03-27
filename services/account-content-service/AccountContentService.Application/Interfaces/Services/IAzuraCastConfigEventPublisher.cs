namespace AccountContentService.Application.Interfaces.Services;

public interface IAzuraCastConfigEventPublisher
{
    Task PublishUpdatedAsync(
        string baseUrl,
        string apiKey,
        bool isActive,
        bool isDeleted,
        DateTime updatedAt,
        CancellationToken cancellationToken);
}
