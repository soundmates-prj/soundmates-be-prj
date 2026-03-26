using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccountContentService.Application.Features.ServiceConfigs.Commands.DeleteAzuraCastConfig;

public sealed class DeleteAzuraCastConfigHandler
    : IRequestHandler<DeleteAzuraCastConfigCommand, bool>
{
    private const string ConfigKey = "azuracast:apikey";

    private readonly ISystemSettingReposiotry _settingRepository;
    private readonly IAzuraCastConfigEventPublisher _eventPublisher;
    private readonly ILogger<DeleteAzuraCastConfigHandler> _logger;

    public DeleteAzuraCastConfigHandler(
        ISystemSettingReposiotry settingRepository,
        IAzuraCastConfigEventPublisher eventPublisher,
        ILogger<DeleteAzuraCastConfigHandler> logger)
    {
        _settingRepository = settingRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteAzuraCastConfigCommand request, CancellationToken cancellationToken)
    {
        var deleted = await _settingRepository.DeleteByKeyAsync(ConfigKey, cancellationToken);

        if (!deleted)
        {
            _logger.LogWarning("AzuraCast config not found for deletion");
            return false;
        }

        _logger.LogInformation("AzuraCast config deleted from SystemSettings");

        try
        {
            await _eventPublisher.PublishUpdatedAsync(
                baseUrl: string.Empty,
                apiKey: string.Empty,
                isActive: false,
                isDeleted: true,
                updatedAt: DateTime.UtcNow,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish AzuraCast deletion event");
        }

        return true;
    }
}
