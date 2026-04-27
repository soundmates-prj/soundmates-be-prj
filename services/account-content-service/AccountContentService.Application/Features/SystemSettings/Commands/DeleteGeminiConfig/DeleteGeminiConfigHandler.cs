using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccountContentService.Application.Features.SystemSettings.Commands.DeleteGeminiConfig;

public sealed class DeleteGeminiConfigHandler
    : IRequestHandler<DeleteGeminiConfigCommand, bool>
{
    private readonly ISystemSettingReposiotry _settingRepository;
    private readonly IGeminiConfigEventPublisher _eventPublisher;
    private readonly ILogger<DeleteGeminiConfigHandler> _logger;

    public DeleteGeminiConfigHandler(
        ISystemSettingReposiotry settingRepository,
        IGeminiConfigEventPublisher eventPublisher,
        ILogger<DeleteGeminiConfigHandler> logger)
    {
        _settingRepository = settingRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<bool> Handle(
        DeleteGeminiConfigCommand request,
        CancellationToken cancellationToken)
    {
        var deleted = await _settingRepository.DeleteByKeyAsync(
            "gemini:apikey",
            cancellationToken);

        if (deleted)
        {
            try
            {
                await _eventPublisher.PublishDeletedAsync("Gemini", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish Gemini config delete event.");
            }
        }

        return deleted;
    }
}
