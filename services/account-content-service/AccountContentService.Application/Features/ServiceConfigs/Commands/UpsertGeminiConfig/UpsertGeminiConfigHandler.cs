using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccountContentService.Application.Features.ServiceConfigs.Commands.UpsertGeminiConfig;

public sealed class UpsertGeminiConfigHandler : IRequestHandler<UpsertGeminiConfigCommand, UpsertGeminiConfigResult>
{
    private readonly IServiceConfigRepository _serviceConfigRepository;
    private readonly IGeminiConfigEventPublisher _eventPublisher;
    private readonly ILogger<UpsertGeminiConfigHandler> _logger;

    public UpsertGeminiConfigHandler(
        IServiceConfigRepository serviceConfigRepository,
        IGeminiConfigEventPublisher eventPublisher,
        ILogger<UpsertGeminiConfigHandler> logger)
    {
        _serviceConfigRepository = serviceConfigRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<UpsertGeminiConfigResult> Handle(UpsertGeminiConfigCommand request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Provider, "Gemini", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("provider must be Gemini");
        }

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            throw new ArgumentException("apiKey is required");
        }

        var now = DateTime.UtcNow;
        var provider = request.Provider.Trim();
        var apiKey = request.ApiKey.Trim();

        await _serviceConfigRepository.UpsertAsync(
            provider,
            apiKey,
            request.IsActive,
            now,
            cancellationToken);

        try
        {
            await _eventPublisher.PublishUpdatedAsync(provider, apiKey, request.IsActive, now, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish Gemini config event. Data was saved in ServiceConfig table.");
        }

        return new UpsertGeminiConfigResult(provider, request.IsActive, now);
    }
}
