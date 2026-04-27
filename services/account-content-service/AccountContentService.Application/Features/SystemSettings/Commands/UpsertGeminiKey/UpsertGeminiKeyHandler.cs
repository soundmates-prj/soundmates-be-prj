using AccountContentService.Application.Interfaces.Repositories;
using AccountContentService.Application.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccountContentService.Application.Features.SystemSettings.Commands.UpsertGeminiKey;

public sealed class UpsertGeminiKeyHandler : IRequestHandler<UpsertGeminiKeyCommand, UpsertGeminiKeyResult>
{
    private readonly ISystemSettingReposiotry _settingRepository;
    private readonly IEncryptionService _encryption;
    private readonly IGeminiConfigEventPublisher _eventPublisher;
    private readonly ILogger<UpsertGeminiKeyHandler> _logger;

    public UpsertGeminiKeyHandler(
        ISystemSettingReposiotry settingRepository,
        IEncryptionService encryption,
        IGeminiConfigEventPublisher eventPublisher,
        ILogger<UpsertGeminiKeyHandler> logger)
    {
        _settingRepository = settingRepository;
        _encryption = encryption;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<UpsertGeminiKeyResult> Handle(UpsertGeminiKeyCommand request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Provider, "Gemini", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Provider must be Gemini");
        }

        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            throw new ArgumentException("ApiKey is required");
        }

        var now = DateTime.UtcNow;
        var key = $"gemini:apikey";
        var encryptedValue = _encryption.Encrypt(request.ApiKey.Trim());

        // Upsert into SystemSettings table with encrypted value
        // isActive encoded in SettingType field: "encrypted:true" or "encrypted:false"
        var settingType = request.IsActive ? "encrypted:true" : "encrypted:false";
        var description = $"Gemini API Key - Provider: {request.Provider.Trim()} - Active: {request.IsActive}";

        await _settingRepository.UpsertAsync(
            key,
            encryptedValue,
            settingType,
            description,
            request.IsActive,
            cancellationToken);

        // Publish event to RabbitMQ so ai-service can update its config
        try
        {
            await _eventPublisher.PublishUpdatedAsync(
                request.Provider.Trim(),
                request.ApiKey.Trim(),
                request.IsActive,
                now,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish Gemini config event. Data was saved in SystemSettings table.");
        }

        return new UpsertGeminiKeyResult(request.Provider, request.IsActive, now);
    }
}
